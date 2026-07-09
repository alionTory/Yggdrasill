using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;
using Quantum;
using Quantum.Menu;
using UnityEngine;

namespace Yggdrasill.Tests.EditMode
{
    public class LocalSimulationRunner
    {
        /// <summary>
        /// Sample Menu 설정 파일.
        /// </summary>
        private const string YggdrasillMenuConfigResourceName = "YggdrasillMenuConfig";

        /// <summary>
        /// 시뮬레이션을 목표 상태까지 진행시킬 때 허용하는 최대 틱 수. 무한 루프 방지를 위한 안전장치.
        /// </summary>
        private const int MaxServiceTicks = 4096;

        private readonly SessionRunner _runner;
        private readonly QuantumGame _game;
        private readonly IDisposable _pollInputSubscription;

        /// <summary>
        /// 시뮬레이션 한 틱의 델타 타임(초). SessionConfig의 UpdateFPS에서 유도한다.
        /// </summary>
        private readonly double _tickDeltaTime;

        // AddPlayer / SendCommand는 서로 다른 프레임에 반영되면 안 되므로(테스트는 단일 검증 프레임을 검사),
        // 실제 세션 조작은 VerifiedFrame이 처음 요청될 때 한 번에 수행한다.
        private readonly List<Int32> _pendingPlayers = new List<Int32>();
        private readonly List<(Int32 playerIndex, DeterministicCommand command)> _pendingCommands = new List<(Int32, DeterministicCommand)>();

        private Frame _verifiedFrame;
        private bool _resolved;

        /// <summary>
        /// 시뮬레이션 시작
        /// </summary>
        public LocalSimulationRunner()
        {
            // EditMode에서는 RuntimeInitializeOnLoad가 실행되지 않으므로 Quantum 기반(룩업 테이블, 네이티브 유틸 등)을 직접 초기화한다.
            QuantumRunnerUnityFactory.Init();

            var menuConfig = LoadMenuConfig();
            var runtimeConfig = BuildRuntimeConfig(menuConfig);
            var sessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig;

            _tickDeltaTime = 1.0 / sessionConfig.UpdateFPS;

            var arguments = new SessionRunner.Arguments
            {
                // GameObject 기반 QuantumRunnerUnityFactory는 EditMode에서 DontDestroyOnLoad/Destroy가 불가하므로
                // 헤드리스 DotNetRunnerFactory를 사용한다. 시뮬레이션에 영향을 주는 설정은 메뉴와 동일하게 유지한다.
                RunnerFactory = new DotNetRunnerFactory(),
                AssetSerializer = new QuantumUnityJsonSerializer(),
                CallbackDispatcher = QuantumCallback.Dispatcher,
                EventDispatcher = QuantumEvent.Dispatcher,
                ResourceManager = QuantumUnityDB.Global,
                RuntimeConfig = runtimeConfig,
                SessionConfig = sessionConfig,
                GameMode = DeterministicGameMode.Local,
                RunnerId = "YggdrasillLocalSimulation",
                PlayerCount = Math.Min(menuConfig.MaxPlayerCount, Quantum.Input.MaxCount),
                DeltaTimeType = SimulationUpdateTime.EngineDeltaTime,
                InstantReplaySettings = InstantReplaySettings.Default,
                RecordingFlags = RecordingFlags.None,
                GameFlags = 0,
            };

            // 로컬 시뮬레이션은 매 틱 입력이 필요하다. 별도 입력 소스가 없으므로 기본(빈) 입력을 반복 입력으로 제공한다.
            _pollInputSubscription = QuantumCallback.SubscribeManual(
                (CallbackPollInput c) => c.SetInput(new Quantum.Input(), DeterministicInputFlags.Repeatable));

            _runner = SessionRunner.Start(arguments);
            _game = (QuantumGame)_runner.DeterministicGame;

            // 첫 검증 프레임이 만들어질 때까지 진행시켜 시뮬레이션을 완전히 기동한다.
            ServiceUntil(() => _game.Frames.Verified != null);
        }

        /// <summary>
        /// 시뮬레이션 종료.
        /// </summary>
        public void ShutDown()
        {
            try
            {
                _pollInputSubscription?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                _runner?.Shutdown();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 로컬 플레이어 추가
        /// </summary>
        public void AddPlayer(Int32 playerIndex)
        {
            EnsureNotResolved();
            _pendingPlayers.Add(playerIndex);
        }

        /// <summary>
        /// <see cref="playerIndex"/>에 해당하는 플레이어가 <see cref="command"/>를 보내도록 함.
        /// </summary>
        public void SendCommand(DeterministicCommand command, Int32 playerIndex)
        {
            EnsureNotResolved();
            _pendingCommands.Add((playerIndex, command));
        }

        /// <summary>
        /// 추가된 모든 플레이어와 전송된 모든 커맨드가 반영된 검증 프레임.
        /// 최초 접근 시 시뮬레이션을 진행시켜 커맨드가 실린 프레임을 확정한다.
        /// </summary>
        public Frame VerifiedFrame
        {
            get
            {
                Resolve();
                return _verifiedFrame;
            }
        }

        /// <summary>
        /// 대기 중인 플레이어 추가와 커맨드 전송을 실제 세션에 반영하고,
        /// 모든 커맨드가 담긴 검증 프레임을 확정한다.
        /// </summary>
        private void Resolve()
        {
            if (_resolved)
            {
                return;
            }
            _resolved = true;

            AddPendingPlayers();
            SendPendingCommands();
            _verifiedFrame = CaptureFrameWithCommands();
        }

        /// <summary>
        /// 대기 중인 로컬 플레이어를 추가하고, 모두 확정(confirm)될 때까지 시뮬레이션을 진행시킨다.
        /// 커맨드는 슬롯이 확정된 뒤에만 전송될 수 있으므로 여기서 반드시 대기한다.
        /// </summary>
        private void AddPendingPlayers()
        {
            if (_pendingPlayers.Count == 0)
            {
                return;
            }

            var confirmed = 0;
            using (QuantumCallback.SubscribeManual(
                       (CallbackLocalPlayerAddConfirmed _) => confirmed++, _game))
            {
                foreach (var playerIndex in _pendingPlayers)
                {
                    _game.AddPlayer(playerIndex, new RuntimePlayer());
                }

                ServiceUntil(() => confirmed >= _pendingPlayers.Count);
            }
        }

        /// <summary>
        /// 대기 중인 모든 커맨드를 (틱 진행 없이) 한 번에 전송하여 동일한 프레임에 실리도록 한다.
        /// </summary>
        private void SendPendingCommands()
        {
            foreach (var (playerIndex, command) in _pendingCommands)
            {
                var result = _game.SendCommand(playerIndex, command);
                if (result != DeterministicCommandSendResult.Success)
                {
                    throw new InvalidOperationException(
                        $"LocalSimulationRunner: 플레이어 {playerIndex}의 커맨드 전송 실패 ({result}).");
                }
            }
        }

        /// <summary>
        /// 전송한 모든 커맨드가 담긴 검증 프레임을 찾을 때까지 시뮬레이션을 진행시켜 해당 프레임을 반환한다.
        /// <see cref="CallbackSimulateFinished"/>로 매 시뮬레이션 프레임을 검사하므로 커맨드가 실린 프레임을 놓치지 않는다.
        /// </summary>
        private Frame CaptureFrameWithCommands()
        {
            Frame captured = null;
            using (QuantumCallback.SubscribeManual(
                       (CallbackSimulateFinished c) =>
                       {
                           if (captured == null && FrameCarriesAllCommands(c.Frame))
                           {
                               captured = c.Frame;
                           }
                       }, _game))
            {
                ServiceUntil(() => captured != null);
            }

            return captured;
        }

        /// <summary>
        /// 전송한 모든 커맨드가 각 플레이어 슬롯에 실린 프레임인지 확인.
        /// 로컬 시뮬레이션은 커맨드를 직렬화 없이 그대로 유지하므로, 이 프레임의 슬롯에는 전송한 인스턴스가 그대로 들어 있다.
        /// (인스턴스 동일성 검증은 테스트의 단언에 맡기고, 여기서는 커맨드가 실린 프레임을 놓치지 않고 확정하는 데 집중한다.)
        /// 대기 중인 커맨드가 없으면 첫 시뮬레이션 프레임을 그대로 사용한다.
        /// </summary>
        private bool FrameCarriesAllCommands(Frame frame)
        {
            foreach (var (playerIndex, _) in _pendingCommands)
            {
                if (frame.GetPlayerCommand(playerIndex) == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// <see cref="condition"/>이 참이 될 때까지 한 틱씩 시뮬레이션을 진행시킨다.
        /// </summary>
        private void ServiceUntil(Func<bool> condition)
        {
            for (var tick = 0; tick < MaxServiceTicks; tick++)
            {
                if (condition())
                {
                    return;
                }

                _runner.Service(_tickDeltaTime);
            }

            if (!condition())
            {
                throw new TimeoutException(
                    $"LocalSimulationRunner: {MaxServiceTicks}틱 안에 목표 상태에 도달하지 못했습니다.");
            }
        }

        /// <summary>
        /// Sample Menu 설정 파일을 로드한다.
        /// </summary>
        private static QuantumMenuConfig LoadMenuConfig()
        {
            var menuConfig = Resources.Load<QuantumMenuConfig>(YggdrasillMenuConfigResourceName);
            if (menuConfig == null)
            {
                throw new InvalidOperationException(
                    $"LocalSimulationRunner: 메뉴 설정 '{YggdrasillMenuConfigResourceName}'을(를) Resources에서 찾을 수 없습니다.");
            }

            return menuConfig;
        }

        /// <summary>
        /// 메뉴 설정의 씬으로부터 <see cref="RuntimeConfig"/>를 만든다.
        /// Sample Menu와 동일하게 <see cref="QuantumMenuConfig.Init"/>로 Resources의 <see cref="QuantumMenuSceneInfo"/>
        /// 애셋들을 로드한 뒤(<see cref="QuantumMenuConfig.AvailableSceneAssets"/>) 사용할 씬을 고른다.
        /// 이후 연결 시퀀스(QuantumMenuConnectionBehaviourSDK.PatchConnectArgs)와 동일하게
        /// 원본을 복제하고, 시드가 0이면 재설정하며, SimulationConfig가 비어 있으면 기본값으로 채운다.
        /// </summary>
        private static RuntimeConfig BuildRuntimeConfig(QuantumMenuConfig menuConfig)
        {
            // Resources 폴더의 QuantumMenuSceneInfo 애셋들을 AvailableSceneAssets로 로드한다.
            menuConfig.Init();

            // 숨김 처리되지 않고 유효한 Map이 지정된 첫 씬을 사용한다.
            var scene = menuConfig.AvailableSceneAssets
                .FirstOrDefault(s => s != null && !s.IsHidden && s.RuntimeConfig != null && s.RuntimeConfig.Map.Id.IsValid);
            if (scene == null)
            {
                throw new InvalidOperationException(
                    "LocalSimulationRunner: 유효한 Map이 설정된 QuantumMenuSceneInfo를 찾을 수 없습니다. " +
                    "YggdrasillSceneInfo.asset의 RuntimeConfig(Map/SimulationConfig/SystemsConfig)를 채워주세요.");
            }

            // 메뉴처럼 원본 RuntimeConfig를 복제하여 원본 애셋을 변경하지 않는다.
            var runtimeConfig = new QuantumUnityJsonSerializer().CloneConfig(scene.RuntimeConfig);

            // 시드가 0이면 매번 새로 뽑는다(메뉴와 동일).
            if (runtimeConfig.Seed == 0)
            {
                runtimeConfig.Seed = Guid.NewGuid().GetHashCode();
            }

            // SimulationConfig가 지정되지 않았다면 글로벌 기본값을 사용한다(메뉴와 동일).
            if (runtimeConfig.SimulationConfig.Id.IsValid == false &&
                QuantumDefaultConfigs.TryGetGlobal(out var defaultConfigs))
            {
                runtimeConfig.SimulationConfig = defaultConfigs.SimulationConfig;
            }

            return runtimeConfig;
        }

        private void EnsureNotResolved()
        {
            if (_resolved)
            {
                throw new InvalidOperationException(
                    "LocalSimulationRunner: VerifiedFrame이 확정된 뒤에는 플레이어/커맨드를 추가할 수 없습니다.");
            }
        }
    }
}
