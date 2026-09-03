using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
using UnityEngine;
using UnityEngine.InputSystem;
using Yggdrasill.TestHelper.Protocol;
using Yggdrasill.Utilities;

namespace Yggdrasill.TestHelper.Debug
{
    public class TestHookServer : MonoBehaviour
    {
        private int? _port;

        private SynchronizationContext? _unityContext;

        private TcpListener? _listener;
        private TcpClient? _client;
        private JsonRpc? _jsonRpc;

        private CancellationTokenSource? _cts;

        /// <summary>
        /// 게임 시작 시 <see cref="TestHookServer"/> 스크립트가 부착된 게임 오브젝트를 하나 생성한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // RuntimeInitializeOnLoadMethod 어트리뷰트가 붙은 static 메서드는 게임 시작 시 한 번 실행된다.

            // 도메인 리로드가 꺼진 상태에서의 중복 생성 방지
            if (FindAnyObjectByType<TestHookServer>() != null)
                return;

            var go = new GameObject("E2E Hook Server");
            go.AddComponent<TestHookServer>();
        }

        /// <summary>
        /// <see cref="TestHookServer"/>가 부착된 게임 오브젝트를 초기화한다.
        /// </summary>
        /// <remarks>
        /// 다음을 수행한다. <br/>
        /// 1. 씬 전환 시에도 게임 오브젝트가 파괴되지 않도록 함.
        /// 2. TCP, StreamJsonRpc 기반 테스트 훅 서버를 연다.
        /// </remarks>
        private async Awaitable Start()
        {
            DontDestroyOnLoad(gameObject);
            _unityContext = SynchronizationContext.Current;
            _cts = new CancellationTokenSource();

            _port = ParseArg(ITestHookApi.PortCommandLineArgumentName);
            if (_port.HasValue)
            {
                // 창이 포커스된 상태가 아니여도 클릭 이벤트가 가능하도록 하기 위해 필요.
                Application.runInBackground = true;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            
                await TcpConnect();
                if(_client != null)
                    JsonRpcConnect();
                
                SetFrameRate();
            }
            else
            {
                UnityEngine.Debug.Log("TestHookServer 게임 오브젝트가 생성되었으나, 포트 번호가 주어지지 않아 서버를 열지 않습니다.");
            }
        }

        /// <summary>
        /// 클라이언트와 TCP 연결을 수립한다.
        /// </summary>
        /// <remarks>
        /// 연결 성공 시 <see cref="_client"/>의 값이 <see cref="TcpClient.GetStream"/> 호출이 가능한 상태로 초기화된다.
        /// <see cref="_cts"/>, <see cref="TcpListener.Stop"/>등으로 도중에 연결이 취소되면, <see cref="_client"/>의 값이 null로 유지된다.
        /// </remarks>
        [MemberNotNull(nameof(_listener))]
        private async Task TcpConnect()
        {
            Contract.RequireNotNull(_port);
            Contract.RequireNotNull(_cts);
            _listener = new TcpListener(IPAddress.Loopback, _port.Value);
            _listener.Start();
            UnityEngine.Debug.Log($"[HOOK] listening on 127.0.0.1:{_port}");
            try
            {
                UnityEngine.Debug.Log("[HOOK] 클라이언트 연결 대기 중...");
                _client = await _listener.AcceptTcpClientAsync();
                UnityEngine.Debug.Log("[HOOK] 클라이언트 연결 완료.");
            }
            catch (ObjectDisposedException)
            {
                UnityEngine.Debug.Log("[HOOK] 클라이언트 연결 취소됨.");
            }
            catch (SocketException) when (_cts.IsCancellationRequested)
            {
                UnityEngine.Debug.Log("[HOOK] 클라이언트 연결 취소됨.");
            }
        }

        /// <summary>
        /// JSON-RPC 연결을 시작한다.
        /// </summary>
        private async Task JsonRpcConnect()
        {
            Contract.RequireNotNull(_client);
            Contract.RequireNotNull(_unityContext);
            _jsonRpc = new JsonRpc(_client.GetStream())
            {
                SynchronizationContext = _unityContext,
            };
            _jsonRpc.AddLocalRpcTarget(new TestHookApi());
            _jsonRpc.StartListening();

            try
            {
                await _jsonRpc.Completion;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[HOOK] 세션 비정상 종료: {ex.Message}");
            }
        }

        /// <summary>
        /// 명령행 인수 값에 따라 프레임 레이트 설정.
        /// </summary>
        /// <remarks>
        /// CI 환경 등에서 CPU 점유율을 낮추는 데 사용 가능.
        /// </remarks>
        private void SetFrameRate()
        {
            var frameRate = ParseArg(ITestHookApi.TargetFrameRateCommandLineArgumentName);
            if (frameRate.HasValue)
            {
                Application.targetFrameRate = frameRate.Value;
                UnityEngine.Debug.Log($"[HOOK] targetFrameRate가 {frameRate.Value}로 설정됨.");
            }
        }

        /// <summary>
        /// 게임 프로세스 실행 시 명령행 인수 <paramref name="argName"/> 에 지정된 정수 값을 반환한다.
        /// </summary>
        /// <returns>
        /// 해당 명령행 인수가 지정되지 않았을 시, null 리턴.
        /// </returns>
        private static int? ParseArg(string argName)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == argName && int.TryParse(args[i + 1], out int value))
                    return value;
            return null;
        }
        
        void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _jsonRpc?.Dispose();
            _client?.Dispose();
            _listener?.Stop();
        }
    }
}
