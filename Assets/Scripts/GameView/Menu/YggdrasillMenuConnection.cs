using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using Quantum.Menu;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    public class YggdrasillMenuConnection : QuantumMenuConnectionBehaviour, IHasInvariants
    {
        private RealtimeClient? _client;
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
        /// 게임이 온라인으로 실행 중인지 여부를 나타냄.
        /// </summary>
        public bool IsOnline { get; private set; } = false;
        
        /// <summary>
        /// 비공개 방 접속 시 참가 코드
        /// </summary>
        public string? InvitationCode { get; private set; } = null;

        public override RealtimeClient? Client => _client;
        public override string? SessionName => _client?.CurrentRoom?.Name;
        public override string? Region => _client?.CurrentRegion;
        public override string? AppVersion => _client?.AppSettings?.AppVersion;

        /// <summary>
        /// 이 프로젝트에서는 플레이어 목록 UI를 쓰지 않으므로 항상 <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// 목록이 필요해지면 <c>QuantumMenuConnectionBehaviourSDK.Usernames</c> 구현을 참고할 것.
        /// </remarks>
        public override List<string>? Usernames => null;

        public override int MaxPlayerCount => _client?.CurrentRoom?.MaxPlayers ?? 0;
        public override bool IsConnected => _client?.IsConnected ?? false;
        public override int Ping => _runner?.Session?.Stats.Ping ?? 0;

        protected override Task<ConnectResult> ConnectAsyncInternal(QuantumMenuConnectArgs connectArgs)
        {
            throw new NotSupportedException(
                $"{nameof(YggdrasillMenuConnection)}을 대상으로 ConnectAsync, ConnectAsyncInternal이 호출되어서는 안 됨. 대신 {nameof(StartLocalAsync)}, {nameof(StartOnlineAsync)}를 호출할 것.");
        }

        public virtual void Invariants()
        {
            Contract.Invariant(IsGameRunning == (_runner != null));
            Contract.Invariant(IsGameRunning == (_cancellation != null));
            Contract.Invariant(IsGameRunning == (_linkedCancellationToken != null));

            Contract.Invariant(!IsOnline || IsGameRunning,
                $"{nameof(IsConnected)}가 true, 즉 온라인으로 게임이 실행 중이라면, {nameof(IsGameRunning)}도 true여야 한다.");
            Contract.Invariant(IsOnline == (Client != null));
            Contract.Invariant(IsOnline || (InvitationCode == null), $"{nameof(IsOnline)}가 false이면, {nameof(InvitationCode)}는 null이어야 한다.");
        }

        /// <summary>
        /// 리전 선택 UI를 쓰지 않으므로 항상 빈 목록을 반환한다.
        /// </summary>
        public override Task<List<QuantumMenuOnlineRegion>>
            RequestAvailableOnlineRegionsAsync(QuantumMenuConnectArgs a)
            => Task.FromResult(new List<QuantumMenuOnlineRegion>());

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
                result = await HandleConnectionFail(e, ConnectionPhase.StartingRunner);
            }

            Invariants();
            return result;
        }

        /// <summary>
        /// Photon 서버에 연결하고, 방에 입장하고, 게임 시뮬레이션을 시작한다.
        /// </summary>
        /// <returns>
        /// <see cref="ConnectResult"/>에 연결 및 게임 시작 성공 여부와, 실패한 경우 실패 원인을 담아 반환한다.
        /// </returns>
        /// <remarks>
        /// 방에 <paramref name="connectArgs.MaxPlayerCount"/>명의 클라이언트가 접속할 때까지 대기(await)한다. <br/>
        /// 방에 인원이 전부 채워진 뒤에 게임 시뮬레이션이 시작된다.
        /// </remarks>
        public async Task<ConnectResult> StartOnlineAsync(QuantumMenuConnectArgs connectArgs)
        {
            Contract.Require(!IsGameRunning);
            Contract.Require(!string.IsNullOrEmpty(connectArgs.Session) || connectArgs.Creating,
                $"{nameof(connectArgs.Session)}이 null또는 빈 문자열인 경우, 즉 자동 매칭을 수행하려는 경우, {nameof(connectArgs.Creating)}이 true여야 함.");

            SetAuthValues(connectArgs);
            SetCancellationToken();

            ConnectResult result;
            ConnectionPhase phase = ConnectionPhase.ConnectingPhoton;
            try
            {
                await ConnectPhotonRoom(connectArgs);
                
                phase = ConnectionPhase.WaitingOpponent;
                await WaitForOpponentAsync(connectArgs.MaxPlayerCount);
                
                phase = ConnectionPhase.StartingRunner;
                await StartSessionRunnerAsync(connectArgs);
                _runner.Game.AddPlayer(0, connectArgs.RuntimePlayers[0]);

                result = ConnectResult.Ok();
            }
            catch (Exception e)
            {
                result = await HandleConnectionFail(e, phase);
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
        /// Photon 서버에 연결하고, 룸에 입장한다.
        /// </summary>
        /// <remarks>
        /// ensure <see cref="IsOnline"/>
        /// </remarks>
        [MemberNotNull(nameof(Client))]
        private async Task ConnectPhotonRoom(QuantumMenuConnectArgs connectArgs)
        {
            Contract.RequireNotNull(_linkedCancellationToken);
            Contract.Require(!IsOnline);

            var matchmakingArguments = new MatchmakingArguments
            {
                PhotonSettings = new AppSettings(connectArgs.AppSettings)
                {
                    AppVersion = connectArgs.AppVersion,
                    FixedRegion = connectArgs.Region
                },
                EmptyRoomTtlInSeconds = connectArgs.ServerSettings.EmptyRoomTtlInSeconds,
                EnableCrc = connectArgs.ServerSettings.EnableCrc,
                PlayerTtlInSeconds = connectArgs.ServerSettings.PlayerTtlInSeconds,
                MaxPlayers = connectArgs.MaxPlayerCount,
                RoomName = connectArgs.Session,
                CanOnlyJoin = !connectArgs.Creating,
                PluginName = connectArgs.PhotonPluginName,
                AsyncConfig = new AsyncConfig()
                {
                    TaskFactory = AsyncConfig.CreateUnityTaskFactory(),
                    CancellationToken = _linkedCancellationToken.Value
                },
                AuthValues = connectArgs.AuthValues,
            };

            ReportProgress("매치메이킹 중...");
            _client = await MatchmakingExtensions.ConnectToRoomAsync(matchmakingArguments);
            IsOnline = true;
            if(!string.IsNullOrEmpty(connectArgs.Session))
                InvitationCode = connectArgs.Session;
        }

        /// <summary>
        /// Photon 룸에 입장한 후, 룸에 총 <paramref name="playerCount"/>명의 클라이언트가 입장할 때까지 대기한다.
        /// </summary>
        private async Task WaitForOpponentAsync(int playerCount)
        {
            Contract.RequireNotNull(_linkedCancellationToken);
            Contract.RequireNotNull(Client);
            Contract.Require(_runner == null,
                $"이 메서드는 Service() 호출 루프를 만드므로, 이중 Service() 펌프를 방지하기 위해 {nameof(_runner)}가 null이어야 함.");

            ReportProgress("상대를 기다리는 중...");

            /*
             * PlayerCount는 _client.Service()가 호출되어야 갱신된다.
             *
             * Service() 호출 주체는 다음 두 가지이다.
             * 1. ConnectToRoomAsync() 실행 중에만 실행되는 임시 Service() 호출 루프
             * 2. SessionRunner가 생성되면, QuantumRunnerBehaviour가 매 프레임 러너에 대해 Service()를 호출하고, 러너가 다시 _client.Service() 호출.
             *
             * 문제는 ConnectToRoomAsync() 호출 이후, SessionRunner 호출 이전까지는 Service()가 호출되지 않는다는 것.
             * 따라서 임시 Service() 호출 루프를 생성하는 ConnectionServiceScope를 사용한다.
             */
            using (new ConnectionServiceScope(Client))
            {
                while (Client.CurrentRoom.PlayerCount < playerCount)
                {
                    await Awaitable.NextFrameAsync(_linkedCancellationToken.Value);
                }
            }
        }


        /// <summary>
        /// <see cref="SessionRunner"/>를 생성한다.
        /// </summary>
        /// <remarks>
        /// <see cref="Client"/>가 null이면 싱글 플레이 모드로 실행하고, null이 아니면 멀티플레이 모드로 실행한다. <br/>
        /// ensure: <see cref="IsGameRunning"/>
        /// </remarks>
        [MemberNotNull(nameof(_runner))]
        private async Task StartSessionRunnerAsync(QuantumMenuConnectArgs connectArgs)
        {
            Contract.RequireNotNull(_linkedCancellationToken);
            Contract.Require(!(Client != null) || !string.IsNullOrEmpty(Client.UserId),
                $"{nameof(Client)}가 null이 아니면, {nameof(Client.UserId)}는 null 또는 빈 문자열이 아니여야 함.");

            var isMultiplay = Client != null;

            var sessionRunnerArgs = new SessionRunner.Arguments
            {
                RunnerFactory = QuantumRunnerUnityFactory.DefaultFactory,
                GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
                ClientId = connectArgs.AuthValues.UserId,
                RuntimeConfig = BuildRuntimeConfig(connectArgs),
                SessionConfig = connectArgs.SessionConfig?.Config ??
                                QuantumDeterministicSessionConfigAsset.DefaultConfig,
                GameMode = isMultiplay ? DeterministicGameMode.Multiplayer : DeterministicGameMode.Local,
                PlayerCount = connectArgs.MaxPlayerCount,
                CancellationToken = _linkedCancellationToken.Value,
                DeltaTimeType = connectArgs.DeltaTimeType,
                StartGameTimeoutInSeconds = connectArgs.StartGameTimeoutInSeconds,
                GameFlags = connectArgs.GameFlags,
                OnShutdown = OnSessionShutdown,
            };

            if (isMultiplay)
                sessionRunnerArgs.Communicator = new QuantumNetworkCommunicator(Client);

            ReportProgress("게임 시작 중...");
            _runner = (QuantumRunner)await SessionRunner.StartAsync(sessionRunnerArgs);
            IsGameRunning = true;
        }


        enum ConnectionPhase
        {
            ConnectingPhoton,
            WaitingOpponent,
            StartingRunner
        }

        /// <summary>
        /// 연결 실패를 처리한다.
        /// </summary>
        /// <remarks>
        /// ensure: <see cref="_cancellation"/> == null <br/>
        /// ensure: <see cref="_linkedCancellationToken"/> == null <br/>
        /// ensure: <see cref="_runner"/> == null <br/>
        /// ensure: !<see cref="IsGameRunning"/> <br/>
        /// ensure: <see cref="Client"/> == null <br/>
        /// ensure: !<see cref="IsOnline"/> <br/>
        /// ensure: <see cref="InvitationCode"/> == null <br/>
        /// </remarks>
        private async Task<ConnectResult> HandleConnectionFail(Exception exception, ConnectionPhase phase)
        {
            Debug.LogException(exception);
            await CleanupAsync();
            return new ConnectResult
            {
                FailReason = InferFailReason(phase),
                DebugMessage = exception.Message,
            };
        }

        /// <summary>
        /// 연결 및 게임 시작 실패 원인을 나타내는 코드를 반환한다.
        /// </summary>
        /// <param name="phase">실패 전까지 진행된 단계</param>
        /// <returns><see cref="ConnectFailReason"/>에 정의된 실패 코드</returns>
        private int InferFailReason(ConnectionPhase phase)
        {
            int failReason;
            if (AsyncConfig.Global.IsCancellationRequested)
                failReason = ConnectFailReason.ApplicationQuit;
            else if (_cancellation != null && _cancellation.IsCancellationRequested)
                failReason = ConnectFailReason.UserRequest;
            else
                failReason = phase switch
                {
                    ConnectionPhase.ConnectingPhoton => ConnectFailReason.ConnectingFailed,
                    ConnectionPhase.WaitingOpponent => ConnectFailReason.ConnectingFailed,
                    ConnectionPhase.StartingRunner => ConnectFailReason.RunnerFailed,
                };

            return failReason;
        }


        protected override async Task DisconnectAsyncInternal(int reason)
        {
            await CleanupAsync();

            Invariants();
        }

        /// <summary>
        /// 연결 및 게임을 종료하며, 자원을 반환하고, 클래스 불변식을 회복한다.
        /// </summary>
        /// <remarks>
        /// ensure: <see cref="_cancellation"/> == null <br/>
        /// ensure: <see cref="_linkedCancellationToken"/> == null <br/>
        /// ensure: <see cref="_runner"/> == null <br/>
        /// ensure: !<see cref="IsGameRunning"/> <br/>
        /// ensure: <see cref="Client"/> == null <br/>
        /// ensure: !<see cref="IsOnline"/> <br/>
        /// ensure: <see cref="InvitationCode"/> == null <br/>
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

            if (_client != null)
                await _client.DisconnectAsync();
            _client = null;
            IsOnline = false;
            InvitationCode = null;
        }
    }
}