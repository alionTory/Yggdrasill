using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Quantum;

namespace Yggdrasill.Tests.EditMode
{
    public class PlantSeedlingSystemTest
    {
        private LocalSimulation _localSimulation;
        private PlantSeedlingSystem _plantSeedlingSystem;

        [SetUp]
        public void Setup()
        {
            _localSimulation = LocalSimulation.Create(SceneList.MultiplayPrototype);
            _plantSeedlingSystem = new PlantSeedlingSystem();
        }

        [TearDown]
        public void TearDown()
        {
            _localSimulation.Shutdown();
        }

        /// <summary>
        /// <see cref="PlantSeedlingSystem"/>이, 존재하는 모든 플레이어의 <see cref="SpawnSeedlingCommand"/>에 대해, <see cref="SpawnSeedlingCommand.Execute"/>를 호출하는지 검증
        /// </summary>
        [TestCase(new bool[] { true, true }, TestName = "Both players sent commands")]
        public void CallExecute(IReadOnlyList<bool> isPlayersSentCommands)
        {
            var commands = new List<SpawnSeedlingCommand>();

            // 플레이어 추가
            for (Int32 playerIndex = 0; playerIndex < isPlayersSentCommands.Count; playerIndex++)
            {
                _localSimulation.AddPlayer(playerIndex);
            }
            _localSimulation.UpdateFrame();
            
            // isPlayersSentCommands 가 true인 플레이어에 대해, 시뮬레이션에 커맨드를 전송.
            for (Int32 playerIndex = 0; playerIndex < isPlayersSentCommands.Count; playerIndex++)
            {
                if (isPlayersSentCommands[playerIndex])
                {
                    var command = Substitute.For<SpawnSeedlingCommand>();
                    commands.Add(command);
                    _localSimulation.SendCommand(playerIndex, command);
                }
            }
            _localSimulation.UpdateFrame();


            var frame = _localSimulation.VerifiedFrame;
            for (Int32 playerIndex = 0; playerIndex < isPlayersSentCommands.Count; playerIndex++)
            {
                var command = frame.GetPlayerCommand<SpawnSeedlingCommand>(playerIndex);
                command.ReceivedWithAnyArgs().Execute(null);
            }
        }
    }
}