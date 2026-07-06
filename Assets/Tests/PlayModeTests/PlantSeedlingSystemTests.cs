using System.Collections;
using NUnit.Framework;
using Quantum;
using UnityEngine;
using UnityEngine.TestTools;

namespace Yggdrasill.Tests.PlayMode
{
    /// <summary>
    /// PlantSeedlingSystem 및 SpawnSeedlingCommand.Execute 에 대한 통합(단위) 테스트.
    ///
    /// 검증 기준은 해당 코드의 주석(명세)이다.
    /// - PlantSeedlingSystem.Update:
    ///   "현재 프레임의 모든 플레이어에 대해 SpawnSeedlingCommand 가 존재하는지 확인하고,
    ///    존재하는 모든 SpawnSeedlingCommand 에 대해 Execute 를 실행."
    /// - SpawnSeedlingCommand.Execute:
    ///   "월드 좌표 WorldPosition 에 묘목을 생성함."
    ///
    /// Frame 은 실제 시뮬레이션 없이 생성할 수 없으므로, MultiplayPrototype 씬의
    /// QuantumRunnerLocalDebug 가 시작하는 로컬 결정론 시뮬레이션에 커맨드를 주입해
    /// 관찰 가능한 계약을 검증한다.
    /// </summary>
    public class PlantSeedlingSystemTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return LocalSimulation.Shutdown();
        }

        [UnityTest]
        public IEnumerator PlayerCommand_IsExecuted_CreatingExactlyOneSeedling()
        {
            // 계약: 존재하는 SpawnSeedlingCommand 에 대해 Execute 가 실행되어 묘목이 생성된다.
            yield return LocalSimulation.Boot();

            int before = LocalSimulation.SeedlingCount();

            LocalSimulation.Game.SendCommand(SpawnSeedlingCommand.CreateFromView(2.5f, 3.5f));

            yield return LocalSimulation.WaitForSeedlingCount(before + 1);

            Assert.AreEqual(before + 1, LocalSimulation.SeedlingCount(),
                "존재하는 커맨드 1건에 대해 정확히 1개의 묘목이 생성되어야 합니다.");
        }

        [UnityTest]
        public IEnumerator Execute_PlacesSeedlingAtCommandWorldPosition()
        {
            // 계약: Execute 는 WorldPosition 좌표에 묘목을 생성한다.
            yield return LocalSimulation.Boot();

            const float x = 1.5f;
            const float y = -4.25f;
            int before = LocalSimulation.SeedlingCount();

            LocalSimulation.Game.SendCommand(SpawnSeedlingCommand.CreateFromView(x, y));

            yield return LocalSimulation.WaitForSeedlingCount(before + 1);

            Assert.IsTrue(LocalSimulation.AnySeedlingNear(x, y, 0.01f),
                "묘목은 커맨드의 WorldPosition 좌표에 생성되어야 합니다.");
        }

        [UnityTest]
        public IEnumerator NoCommand_CreatesNoSeedling()
        {
            // 계약: 커맨드가 존재하지 않으면 어떤 묘목도 생성되지 않는다.
            yield return LocalSimulation.Boot();

            int before = LocalSimulation.SeedlingCount();

            // 커맨드를 보내지 않고 여러 틱을 진행시킨다.
            var startNumber = LocalSimulation.Verified().Number;
            while (LocalSimulation.Verified().Number < startNumber + 10)
            {
                yield return null;
            }

            Assert.AreEqual(before, LocalSimulation.SeedlingCount(),
                "커맨드가 없으면 묘목이 생성되지 않아야 합니다.");
        }

        [UnityTest]
        public IEnumerator MultipleCommands_CreateMultipleSeedlings()
        {
            // 계약: "존재하는 모든 SpawnSeedlingCommand 에 대해" Execute 가 실행된다.
            // 서로 다른 틱에 여러 커맨드를 보내면 각각에 대해 묘목이 생성되어야 한다.
            yield return LocalSimulation.Boot();

            int before = LocalSimulation.SeedlingCount();

            LocalSimulation.Game.SendCommand(SpawnSeedlingCommand.CreateFromView(0.5f, 0.5f));
            yield return LocalSimulation.WaitForSeedlingCount(before + 1);

            LocalSimulation.Game.SendCommand(SpawnSeedlingCommand.CreateFromView(-0.5f, -0.5f));
            yield return LocalSimulation.WaitForSeedlingCount(before + 2);

            Assert.AreEqual(before + 2, LocalSimulation.SeedlingCount(),
                "커맨드 2건에 대해 묘목 2개가 생성되어야 합니다.");
        }
    }
}
