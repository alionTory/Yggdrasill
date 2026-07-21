using System;
using System.Threading.Tasks;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using Quantum.Menu;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using QuantumUser.View;
using Input = UnityEngine.Input;

namespace Yggdrasill.Tests.EditMode
{
    public class LocalSimulation
    {
        private SessionRunner _runner;
        private QuantumGame _game;

        private LocalSimulation()
        {
        }

        private void Initialize(SceneInfo sceneInfo)
        {
            var sessionRunnerArguments = new SessionRunner.Arguments
            {
                RunnerFactory = new DotNetRunnerFactory(),
                GameParameters = QuantumRunnerUnityFactory.CreateGameParameters,
                ClientId = Guid.NewGuid().ToString(),
                RuntimeConfig = sceneInfo.runtimeConfig,
                SessionConfig = QuantumDeterministicSessionConfigAsset.DefaultConfig,
                GameMode = DeterministicGameMode.Local,
                PlayerCount = Quantum.Input.MAX_COUNT,
                DeltaTimeType = SimulationUpdateTime.Default,
                GameFlags = 0,
            };

            try
            {
                _runner = SessionRunner.Start(sessionRunnerArguments);
                _game = (QuantumGame)_runner.DeterministicGame;
                _runner.Service(0);  // SessionRunner를 Stating에서 Running 상태로 전환하기 위해 필요.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 주어진 씬에 대해 새 로컬 Quantum 시뮬레이션을 시작한다.
        /// </summary>
        /// <remarks>
        /// require sceneInfo.hasRuntimeConfig
        /// </remarks>
        public static LocalSimulation Create(SceneInfo sceneInfo)
        {
            var result = new LocalSimulation();
            result.Initialize(sceneInfo);
            return result;
        }

        /// <summary>
        /// Quantum 시뮬레이션을 종료하고, 관련 메모리를 해제한다.
        /// </summary>
        /// <remarks>
        /// Quantum은 프레임 상태를 언매니지드 힙에 저장한다. 해당 메모리는 C# GC가 회수하지 않는다. 따라서 시뮬레이션 종료 시 <see cref="Shutdown"/>을 반드시 호출해야 한다. <br/>
        /// 테스트 시 [TearDown]에서 호출하여, 테스트 실패 시에도 호출되도록 할 것.
        /// </remarks>
        public void Shutdown()
        {
            _runner.Shutdown();
        }

        public Frame VerifiedFrame => _game.Frames.Verified;

        public void AddPlayer(int playerSlot)
        {
            _game.AddPlayer(playerSlot, new RuntimePlayer());
        }

        public void SendCommand(int playerSlot, DeterministicCommand command)
        {
            _game.SendCommand(playerSlot, command);
        }

        public void UpdateFrame()
        {
            _runner.Service(1.0 / _runner.Session.SessionConfig.UpdateFPS);
        }
        
    }
}