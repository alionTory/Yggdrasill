using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Yggdrasill.Tests.PlayMode
{
    /// <summary>
    /// PlayMode 통합 테스트용 헬퍼.
    ///
    /// MultiplayPrototype(게임) 씬에는 QuantumRunnerLocalDebug 가 있어,
    /// 씬을 로드하면 네트워크 없이 로컬 결정론 시뮬레이션(DeterministicGameMode.Local)이
    /// 시작된다. 이를 이용해 커맨드를 보내고 프레임 상태를 관찰한다.
    /// </summary>
    public static class LocalSimulation
    {
        public const string GameSceneName = "MultiplayPrototype";

        /// <summary>
        /// 게임 씬을 로드하고 로컬 시뮬레이션이 시작되어 첫 검증 프레임과
        /// 로컬 플레이어가 준비될 때까지 대기한다.
        /// </summary>
        public static IEnumerator Boot(float timeoutSeconds = 20f)
        {
            QuantumRunner.ShutdownAll();
            while (QuantumRunner.ActiveRunners.Any())
            {
                yield return null;
            }

            SceneManager.LoadScene(GameSceneName);
            yield return null; // 씬 로드 및 QuantumRunnerLocalDebug.Start() 실행 대기

            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!IsReady() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsNotNull(QuantumRunner.Default,
                "MultiplayPrototype 씬에서 로컬 Quantum 러너가 시작되어야 합니다.");
            Assert.IsNotNull(QuantumRunner.Default.Game,
                "Quantum 게임이 시작되어야 합니다.");
            Assert.IsNotNull(Verified(),
                "검증된 프레임이 존재해야 합니다.");

            // 시뮬레이션이 실제로 진행되고(=로컬 플레이어가 추가되고 틱이 진행)
            // 커맨드를 받을 준비가 되도록 검증 프레임이 몇 틱 전진할 때까지 대기한다.
            var startNumber = Verified().Number;
            var tickDeadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Verified().Number < startNumber + 2 && Time.realtimeSinceStartup < tickDeadline)
            {
                yield return null;
            }
        }

        public static IEnumerator Shutdown()
        {
            QuantumRunner.ShutdownAll();
            while (QuantumRunner.ActiveRunners.Any())
            {
                yield return null;
            }
        }

        private static bool IsReady()
        {
            var runner = QuantumRunner.Default;
            return runner != null
                   && runner.Game != null
                   && runner.Game.Frames.Verified != null;
        }

        public static QuantumGame Game => QuantumRunner.Default.Game;

        public static Frame Verified()
        {
            return QuantumRunner.Default != null && QuantumRunner.Default.Game != null
                ? QuantumRunner.Default.Game.Frames.Verified
                : null;
        }

        /// <summary>
        /// 현재 검증 프레임에서 Transform2D(=묘목 엔티티)의 개수를 센다.
        /// </summary>
        public static int SeedlingCount()
        {
            var frame = Verified();
            if (frame == null)
            {
                return 0;
            }

            int count = 0;
            foreach (var _ in frame.GetComponentIterator<Transform2D>())
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// 주어진 월드 좌표(x, y) 근처에 Transform2D 엔티티가 존재하는지 확인한다.
        /// </summary>
        public static bool AnySeedlingNear(float x, float y, float tolerance)
        {
            var frame = Verified();
            if (frame == null)
            {
                return false;
            }

            foreach (var (_, transform) in frame.GetComponentIterator<Transform2D>())
            {
                var position = transform.Position;
                if (Math.Abs(position.X.AsFloat - x) <= tolerance &&
                    Math.Abs(position.Y.AsFloat - y) <= tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 검증 프레임의 Transform2D 개수가 target 이상이 될 때까지 대기한다.
        /// </summary>
        public static IEnumerator WaitForSeedlingCount(int target, float timeoutSeconds = 8f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SeedlingCount() < target && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }
    }
}
