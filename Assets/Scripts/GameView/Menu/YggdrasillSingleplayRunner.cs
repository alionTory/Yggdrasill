using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using Quantum.Menu;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Events;

namespace QuantumUser.View.Menu
{
    public class YggdrasillSingleplayRunner : IHasInvariants
    {
        private QuantumRunner? _runner;

        /// <summary>
        /// 사용자 요청에 따라 photon 서버 연결 및 시뮬레이션 실행을 취소하는 용도.
        /// </summary>
        private CancellationTokenSource? _cancellation;

        /// <summary>
        /// <see cref="_cancellation"/> 취소 또는 애플리케이션 종료 시 취소되는 토큰.
        /// </summary>
        private CancellationToken? _linkedCancellationToken;

        public bool IsGameRunning { get; private set; } = false;

        /// <summary>
        /// A Unity event that can be used to receive progress updates in text form.
        /// </summary>
        private UnityEvent<string>? _onProgress;

        /// <summary>
        /// Reports progress to the progress event.
        /// </summary>
        private void ReportProgress(string message)
        {
            _onProgress?.Invoke(message);
        }

        /// <summary>
        /// Register to get notified on session runner shutdowns to handle unexpected errors.
        /// </summary>
        public event Action<ShutdownCause, SessionRunner> SessionShutdownEvent;

        /// <summary>
        /// Is added as callback for <see cref="SessionRunner.Arguments.OnShutdown"/>.
        /// Triggers <see cref="SessionShutdownEvent"/>.
        /// </summary>
        private void OnSessionShutdown(ShutdownCause shutdownCause, SessionRunner sessionRunner)
        {
            SessionShutdownEvent?.Invoke(shutdownCause, sessionRunner);
        }

        public virtual void Invariants()
        {
            Contract.Invariant(IsGameRunning == (_runner != null));
            Contract.Invariant(IsGameRunning == (_cancellation != null));
            Contract.Invariant(IsGameRunning == (_linkedCancellationToken != null));
        }

        private static RuntimeConfig BuildRuntimeConfig(QuantumMenuConnectArgs args)
        {
            // 씬 에셋의 RuntimeConfig를 JSON 왕복으로 깊은 복사 (원본 에셋 오염 방지)
            var config = JsonUtility.FromJson<RuntimeConfig>(
                JsonUtility.ToJson(args.Scene.RuntimeConfig));

            // 시드가 0이면 새로 생성
            if (config.Seed == 0)
                config.Seed = Guid.NewGuid().GetHashCode();

            return config;
        }


        /// <summary>
        /// 게임 시뮬레이션을 오프라인으로 시작한다.
        /// </summary>
        /// <returns>
        /// <see cref="ConnectResult"/>에 게임 시작 성공 여부와, 실패한 경우 실패 원인을 담아 반환한다.
        /// </returns>
        public async Task<ConnectResult> StartLocalAsync(QuantumMenuConnectArgs connectArgs)
        {
            Contract.Require(!IsGameRunning);

            ReportProgress("게임을 싱글 플레이로 시작하는 중...");

            SetAuthValues(connectArgs);
            SetCancellationToken();

            ConnectResult result;
            try
            {
                await StartSessionRunnerAsync(connectArgs);

                for (int i = 0; i < connectArgs.MaxPlayerCount; i++)
                    _runner.Game.AddPlayer(i, new RuntimePlayer { PlayerNickname = $"Player{i + 1}" });

                result = ConnectResult.Ok();
            }
            catch (Exception e)
            {
                result = await HandleConnectionFail(e);
            }

            Invariants();
            return result;
        }

        private static void SetAuthValues(QuantumMenuConnectArgs connectArgs)
        {
            connectArgs.AuthValues = new AuthenticationValues { UserId = Guid.NewGuid().ToString() };
        }

        [MemberNotNull(nameof(_cancellation), nameof(_linkedCancellationToken))]
        private void SetCancellationToken()
        {
            _cancellation = new CancellationTokenSource();
            _linkedCancellationToken = AsyncSetup.CreateLinkedSource(_cancellation.Token).Token;
        }

        /// <summary>
        /// <see cref="SessionRunner"/>를 로컬로 실행한다.
        /// </summary>
        /// <remarks>
        /// ensure: <see cref="IsGameRunning"/>
        /// </remarks>
        [MemberNotNull(nameof(_runner))]
        private async Task StartSessionRunnerAsync(QuantumMenuConnectArgs connectArgs)
        {
            Contract.RequireNotNull(_linkedCancellationToken);

            var sessionRunnerArgs = new SessionRunner.Arguments
            {
                RunnerFactory = QuantumRunnerUnityFactory.DefaultFactory,
                GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
                ClientId = connectArgs.AuthValues.UserId,
                RuntimeConfig = BuildRuntimeConfig(connectArgs),
                SessionConfig = connectArgs.SessionConfig?.Config ??
                                QuantumDeterministicSessionConfigAsset.DefaultConfig,
                GameMode = DeterministicGameMode.Local,
                PlayerCount = connectArgs.MaxPlayerCount,
                CancellationToken = _linkedCancellationToken.Value,
                DeltaTimeType = connectArgs.DeltaTimeType,
                StartGameTimeoutInSeconds = connectArgs.StartGameTimeoutInSeconds,
                GameFlags = connectArgs.GameFlags,
                OnShutdown = OnSessionShutdown,
            };

            ReportProgress("게임 시작 중...");
            _runner = (QuantumRunner)await SessionRunner.StartAsync(sessionRunnerArgs);
            IsGameRunning = true;
        }



        /// <summary>
        /// 게임 시작 실패를 처리한다.
        /// </summary>
        /// <remarks>
        /// ensure: <see cref="_cancellation"/> == null <br/>
        /// ensure: <see cref="_linkedCancellationToken"/> == null <br/>
        /// ensure: <see cref="_runner"/> == null <br/>
        /// ensure: !<see cref="IsGameRunning"/> <br/>
        /// </remarks>
        private async Task<ConnectResult> HandleConnectionFail(Exception exception)
        {
            Debug.LogException(exception);
            await CleanupAsync();
            return new ConnectResult
            {
                FailReason = InferFailReason(),
                DebugMessage = exception.Message,
            };
        }

        /// <summary>
        /// 게임 시작 실패 원인을 나타내는 코드를 반환한다.
        /// </summary>
        /// <returns><see cref="ConnectFailReason"/>에 정의된 실패 코드</returns>
        private int InferFailReason()
        {
            int failReason;
            if (AsyncConfig.Global.IsCancellationRequested)
                failReason = ConnectFailReason.ApplicationQuit;
            else if (_cancellation != null && _cancellation.IsCancellationRequested)
                failReason = ConnectFailReason.UserRequest;
            else
                failReason = ConnectFailReason.RunnerFailed;

            return failReason;
        }


        public async Task DisconnectAsync()
        {
            await CleanupAsync();

            Invariants();
        }

        /// <summary>
        /// 게임을 종료하고, 자원을 반환하고, 클래스 불변식을 회복한다.
        /// </summary>
        /// <remarks>
        /// ensure: <see cref="_cancellation"/> == null <br/>
        /// ensure: <see cref="_linkedCancellationToken"/> == null <br/>
        /// ensure: <see cref="_runner"/> == null <br/>
        /// ensure: !<see cref="IsGameRunning"/> <br/>
        /// </remarks>
        private async Task CleanupAsync()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
            _linkedCancellationToken = null;

            if (_runner != null)
                await _runner.ShutdownAsync();
            _runner = null;
            IsGameRunning = false;
        }
    }
}