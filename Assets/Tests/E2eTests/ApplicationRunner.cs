using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using StreamJsonRpc;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tests.E2eTests
{
    public class ApplicationRunner : IDisposable
    {
        private int _port;
        private Process _process = null!;
        private TcpClient _tcpClient = null!;
        private JsonRpc _jsonRpc = null!;
        private ITestHookApi _testHookApi = null!;

        private static readonly string BuildPath = GetBuildPath();

        private static string ProjectRoot()
        {
            var assetsPath = Application.dataPath;
            var projectRoot = new DirectoryInfo(assetsPath).Parent;
            if (projectRoot != null)
                return projectRoot.FullName;
            else
                return Directory.GetDirectoryRoot(Application.dataPath);
        }

        private static string GetBuildPath()
        {
            return Path.Combine(ProjectRoot(), "Builds", "Windows-Development", "Yggdrasill.exe");
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public static async Task<ApplicationRunner> StartAsync()
        {
            var result = new ApplicationRunner();
            try
            {
                await result.InitializeAsync();
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("ApplicationRunner 초기화 실패");
                result.Dispose();
                throw;
            }
        }

        private ApplicationRunner()
        {
        }

        private async Task InitializeAsync()
        {
            _port = GetFreePort();
            Debug.Log($"포트 {_port}에서 게임 애플리케이션 시작 중.");

            var processInfo = new ProcessStartInfo()
            {
                FileName = BuildPath,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                ArgumentList =
                {
                    $"{TestHookServer.TestHookPortCommandLineArgumentName}",
                    $"{_port}",
                    "-logfile",
                    "-",
                }
            };

            _process = new Process { StartInfo = processInfo };

            // 게임 프로세스 로그를 테스트 드라이버 로그에 전달
            _process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log($"[게임 프로세스 {_port}] {e.Data}");
            };
            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogWarning($"[게임 프로세스 {_port}] {e.Data}");
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
            Debug.Log("TCP 연결 시도 시작...");
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
                    catch (Exception ex)
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
                Debug.Log("TCP 연결 성공.");
            }
            else
            {
                throw new TimeoutException("연결 실패. 타임아웃.");
            }
        }

        public async Task ClickQuickPlayButton()
        {
            Debug.Log("Quick Play 버튼 클릭 시도 중.");
            await _testHookApi.ClickObject(GameObjectId.QuickPlayButton);
            Debug.Log("Quick Play 버튼 클릭 완료");
        }

        /// <summary>
        /// 게임 클라이언트가 멀티플레이 게임 시뮬레이션에 진입할 때까지 대기한다. <br/>
        /// </summary>
        /// <param name="timeout">대기 시간. 이 시간을 넘으면 예외 발생.</param>
        public async Task WaitUntilGameEntrance(TimeSpan timeout)
        {
            Debug.Log("게임 씬 입장을 기다리는 중.");
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            await _testHookApi.WaitUntilSceneLoad(SceneList.MultiplayPrototype, cancellationTokenSource.Token);
            Debug.Log("게임 씬 입장 완료.");
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