using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamJsonRpc;

namespace Tests.E2eTests
{
    public class ApplicationRunner : IDisposable
    {
        private int _port;
        private Process _process = null!;
        private TcpClient _tcpClient = null!;
        private JsonRpc _jsonRpc = null!;
        private ITestHookApi _testHookApi = null!;

        /// <summary>
        /// 리포지터리 루트 디렉터리에 놓여 있는 표식 파일의 이름.
        /// </summary>
        private const string RepositoryRootMarkerFileName = ".repository_root";

        /// <summary>
        /// 게임 프로세스에 덧붙일 명령행 인수를 공백으로 구분해 지정하는 환경 변수의 이름.
        /// </summary>
        /// <remarks>
        /// 예: YGGDRASILL_E2E_GAME_ARGS="-batchmode -nographics -screen-width 1920 -screen-height 1080"
        /// </remarks>
        private const string ExtraGameArgumentsEnvironmentVariable = "YGGDRASILL_E2E_GAME_ARGS";

        private static readonly string BuildPath = GetBuildPath();

        /// <summary>
        /// 리포지터리 루트 디렉터리의 절대 경로를 반환한다.
        /// </summary>
        /// <exception cref="InvalidOperationException">리포지터리 루트를 찾지 못하면 예외 발생.</exception>
        private static string RepositoryRoot()
        {
            var assemblyPath = typeof(ApplicationRunner).Assembly.Location;
            if (string.IsNullOrEmpty(assemblyPath))
                throw new InvalidOperationException(
                    "이 어셈블리의 파일 경로를 알 수 없어 리포지터리 루트를 찾을 수 없습니다.");

            var directory = new DirectoryInfo(Path.GetDirectoryName(assemblyPath)!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, RepositoryRootMarkerFileName)))
                    return directory.FullName;
                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                $"리포지터리 루트를 찾을 수 없습니다. '{assemblyPath}'의 상위 디렉터리 중 " +
                $"'{RepositoryRootMarkerFileName}' 파일을 가진 디렉터리가 없습니다.");
        }

        private static string GetBuildPath()
        {
            const string BuildPathParameterName = "BuildPath";
            var buildPath = TestContext.Parameters[BuildPathParameterName];
            if (buildPath == null)
                throw new InvalidOperationException(
                    $"(리포지터리 루트 기준으로) 게임 빌드 실행 파일의 상대 경로를 지정하는 테스트 파라미터 {BuildPathParameterName}가 주어지지 않았습니다.");
            return Path.Combine(RepositoryRoot(), TestContext.Parameters[BuildPathParameterName]);
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        /// <summary>
        /// 게임 프로세스에 덧붙일 명령행 인수를 환경 변수에서 읽는다.
        /// </summary>
        /// <remarks>
        /// 환경변수가 지정되지 않으면 아무것도 덧붙이지 않는다.
        /// </remarks>
        private static IEnumerable<string> ExtraGameArguments()
        {
            var raw = Environment.GetEnvironmentVariable(ExtraGameArgumentsEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public static async Task<ApplicationRunner> StartAsync(string? photonAppVersion = null)
        {
            var result = new ApplicationRunner();
            try
            {
                await result.InitializeAsync(photonAppVersion);
                return result;
            }
            catch (Exception)
            {
                TestContext.Progress.WriteLine("ApplicationRunner 초기화 실패");
                result.Dispose();
                throw;
            }
        }

        private ApplicationRunner()
        {
        }

        private async Task InitializeAsync(string? photonAppVersion)
        {
            _port = GetFreePort();
            TestContext.Progress.WriteLine($"포트 {_port}에서 게임 애플리케이션 시작 중.");

            var processInfo = new ProcessStartInfo()
            {
                FileName = BuildPath,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                // 유니티는 로그를 UTF-8로 출력한다.
                // 이를 아래와 같이 명시하지 않으면, 콘솔 기본 인코딩으로 디코딩된다.
                // 그러면 UTF-8을 사용하지 않는 콘솔에서 한글이 깨질 수 있다.
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                ArgumentList =
                {
                    ITestHookApi.PortCommandLineArgumentName,
                    $"{_port}",
                    "-logfile",
                    "-",
                }
            };

            if (photonAppVersion != null)
            {
                processInfo.ArgumentList.Add(ITestHookApi.PhotonAppVersionCommandLineArgumentName);
                processInfo.ArgumentList.Add(photonAppVersion);
            }
            
            foreach (var argument in ExtraGameArguments())
                processInfo.ArgumentList.Add(argument);

            _process = new Process { StartInfo = processInfo };

            // 게임 프로세스 로그를 테스트 드라이버 로그에 전달.
            //
            // 여기서 TestContext.Out (또는 Out 생략)을 쓰면 안 된다.
            // TestContext.Out은 BeginOutputReadLine을 호출한 시점에서 실행 중인 테스트 컨텍스트를 물려받는다.
            // 이는 로그가 기록되지 않도록 만들 수 있다. 예를 들어, 게임 프로세스가 [OneTimeSetUp]에서 시작한다면,
            // 그 컨텍스트는 개별 테스트가 아니라 픽스처이고, 픽스처 수준 출력은 .trx 파일에 기록되지 않아 로그가 통째로 사라진다.
            //
            // 대신 현재 실행 중인 테스트의 컨텍스트에 귀속되지 않는 TestContext.Progress를 써야 한다.
            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    TestContext.Progress.WriteLine($"[게임 프로세스 {_port}] {e.Data}");
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    TestContext.Progress.WriteLine($"[게임 프로세스 {_port} - stderr!] {e.Data}");
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            await TcpConnectAsync();

            _jsonRpc = JsonRpc.Attach(_tcpClient.GetStream());
            _testHookApi = _jsonRpc.Attach<ITestHookApi>();
        }

        private async Task TcpConnectAsync()
        {
            TestContext.Progress.WriteLine("TCP 연결 시도 시작...");
            _tcpClient = new TcpClient();

            bool canceled = false;
            bool connected = false;
            using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                cancellationTokenSource.Token.Register(() =>
                {
                    _tcpClient.Close();
                    canceled = true;
                });
                while (!connected && !canceled)
                {
                    try
                    {
                        await _tcpClient.ConnectAsync(IPAddress.Loopback, _port);
                        connected = true;
                    }
                    catch (Exception)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }
                }
            }

            if (!canceled)
            {
                Assert.That(connected, Is.True);
                TestContext.Progress.WriteLine("TCP 연결 성공.");
            }
            else
            {
                throw new TimeoutException("연결 실패. 타임아웃.");
            }
        }

        /**
         * 게임 오브젝트를 클릭한다.
         */
        public async Task Click(GameObjectId gameObjectId)
        {
            await _testHookApi.ClickObject(gameObjectId);
        }

        public async Task InputText(string text)
        {
            await _testHookApi.InputText(text);
        }

        /// <summary>
        /// 게임 오브젝트가 씬에 생성될 때까지 대기한다.
        /// </summary>
        public async Task WaitGameObjectLoad(GameObjectId id, TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromSeconds(10);
            TestContext.WriteLine($"게임 오브젝트 {id}가 생성되기를 기다리는 중.");
            using var cancellationTokenSource = new CancellationTokenSource(timeout.Value);
            await _testHookApi.WaitGameObjectLoad(id, cancellationTokenSource.Token);
            TestContext.WriteLine($"게임 오브젝트 {id} 생성 확인 완료.");
        }

        /// <summary>
        /// 게임 클라이언트가 멀티플레이 게임 시뮬레이션에 진입할 때까지 대기한다. <br/>
        /// </summary>
        /// <param name="timeout">대기 시간. 이 시간을 넘으면 예외 발생.</param>
        public async Task WaitUntilGameEntrance(TimeSpan timeout)
        {
            TestContext.WriteLine("게임 씬 입장을 기다리는 중.");
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            await _testHookApi.WaitUntilSceneLoad(SceneId.MultiplayPrototype, cancellationTokenSource.Token);
            TestContext.WriteLine("게임 씬 입장 완료.");
        }

        public async Task<string> GetInvitationCode()
        {
            return await _testHookApi.GetInvitationCode();
        }

        /// <summary>
        /// 격자(타일맵)의 <paramref name="column"/>열 <paramref name="row"/>행 칸의 중앙을 클릭한다.
        /// </summary>
        /// <param name="column">가장 왼쪽 열의 칸이 1.</param>
        /// <param name="row">가장 아래 행의 칸이 1.</param>
        public async Task ClickTile(int column, int row)
        {
            TestContext.WriteLine($"타일 ({column}, {row}) 클릭 시도 중.");
            await _testHookApi.ClickTile(column, row);
            TestContext.WriteLine($"타일 ({column}, {row}) 클릭 완료.");
        }

        /// <summary>
        /// 현재 격자(타일맵)의 <paramref name="column"/>열 <paramref name="row"/>행 칸에 묘목이 존재하는지 확인한다. <br/>
        /// </summary>
        /// <param name="column">가장 왼쪽 열의 칸이 1.</param>
        /// <param name="row">가장 아래 행의 칸이 1.</param>
        /// <returns>
        /// 묘목이 존재하면 true, 존재하지 않으면 false.
        /// </returns>
        public async Task<bool> IsSeedlingExistInTile(int column, int row)
        {
            return await _testHookApi.IsSeedlingExistInTile(column, row);
        }

        /// <summary>
        /// 격자(타일맵)의 <paramref name="column"/>열 <paramref name="row"/>행 칸에 묘목이 존재할 때까지 대기한다. <br/>
        /// </summary>
        /// <param name="column">가장 왼쪽 열의 칸이 1.</param>
        /// <param name="row">가장 아래 행의 칸이 1.</param>
        /// <param name="timeout">
        /// 대기 시간. 묘목은 서버를 거쳐 생성되므로 즉시 나타나지 않는다.
        /// 이 시간 안에 묘목이 나타나면 true, 나타나지 않으면 false를 리턴한다.
        /// </param>
        public async Task<bool> IsSeedlingExistInTileUntilTimeout(int column, int row, TimeSpan timeout)
        {
            TestContext.WriteLine($"타일 ({column}, {row})에 묘목이 생성되기를 기다리는 중.");
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            try
            {
                await _testHookApi.WaitUntilSeedlingExistInTile(column, row, cancellationTokenSource.Token);
                TestContext.WriteLine($"타일 ({column}, {row})에 묘목 존재 확인.");
                return true;
            }
            catch (OperationCanceledException)
            {
                TestContext.WriteLine($"타임아웃. 타일 ({column}, {row})에 묘목이 존재하지 않음.");
                return false;
            }
        }

        /// <summary>
        /// 이 게임 클라이언트에 존재하는 묘목의 총 개수를 조회한다.
        /// </summary>
        public async Task<int> GetSeedlingCount()
        {
            return await _testHookApi.GetSeedlingCount();
        }

        public void Dispose()
        {
            _jsonRpc?.Dispose();
            _tcpClient?.Dispose();
            _process?.Kill();
            _process?.Dispose();
        }
    }
}