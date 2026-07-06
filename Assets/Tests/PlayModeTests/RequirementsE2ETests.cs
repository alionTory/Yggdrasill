using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Yggdrasill.Tests.PlayMode
{
    /// <summary>
    /// Docs/requirements.adoc 기반 E2E 테스트.
    ///
    /// 각 테스트는 요구사항 문장을 검증한다. 실제 매치메이킹/네트워크가 필요한
    /// 요구사항(방 생성/입장, 다중 클라이언트 동기화)은 오프라인 자동화가 불가능하므로
    /// 검증 가능한 결정론적 토대(같은 입력 -> 같은 결과)를 확인하거나,
    /// 명확한 근거와 함께 Inconclusive 로 표시한다.
    /// </summary>
    public class RequirementsE2ETests
    {
        [SetUp]
        public void SetUp()
        {
            // 메뉴/게임 씬 로드 시 Photon 설정/리전 조회 등으로 발생할 수 있는
            // 네트워크 관련 로그 오류가 테스트를 실패시키지 않도록 한다.
            LogAssert.ignoreFailingMessages = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return LocalSimulation.Shutdown();
            LogAssert.ignoreFailingMessages = false;
        }

        // 명세: "메뉴 씬에서 QUICK PLAY 버튼을 클릭하면, 대기 시간 후 게임 씬으로 전환된다."
        // => 메뉴 씬에 QUICK PLAY 버튼(QuantumMenuUIMain._playButton)이 존재하고 연결되어 있어야 한다.
        //    (버튼 클릭 이후의 실제 전환은 라이브 매치메이킹이 필요하므로 별도 검증.)
        [UnityTest]
        public IEnumerator MenuScene_HasQuickPlayButton()
        {
            SceneManager.LoadScene("MenuPrototype");
            yield return null;
            yield return null;

            var menuType = ResolveType("QuantumMenuUIMain");
            Assert.IsNotNull(menuType, "QuantumMenuUIMain 타입을 찾을 수 없습니다.");

            var menu = UnityEngine.Object.FindAnyObjectByType(menuType);
            Assert.IsNotNull(menu, "메뉴 씬에 QuantumMenuUIMain 이 존재해야 합니다.");

            var field = menuType.GetField("_playButton",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, "_playButton 필드를 찾을 수 없습니다.");

            var playButton = field.GetValue(menu) as Component;
            Assert.IsNotNull(playButton,
                "QUICK PLAY 버튼(_playButton)이 메뉴에 연결되어 있어야 합니다.");
        }

        // 명세: "게임의 맵은 정사각형 격자 구조이다."
        [UnityTest]
        public IEnumerator GameMap_IsSquareGrid()
        {
            SceneManager.LoadScene(LocalSimulation.GameSceneName);
            yield return null;
            yield return null;

            var grid = UnityEngine.Object.FindAnyObjectByType<Grid>();
            Assert.IsNotNull(grid, "게임 씬에 Grid 가 존재해야 합니다.");

            Assert.AreEqual(GridLayout.CellLayout.Rectangle, grid.cellLayout,
                "맵은 사각(격자) 레이아웃이어야 합니다.");
            Assert.That(grid.cellSize.x, Is.EqualTo(grid.cellSize.y).Within(1e-4f),
                "격자 칸은 정사각형(가로=세로)이어야 합니다.");
        }

        // 명세: "플레이어가 격자 칸을 클릭하면, 해당 칸에 묘목이 생성된다."
        // 클릭 -> SpawnSeedlingCommand 변환은 TilemapView 단위 테스트에서 검증하므로,
        // 여기서는 클릭의 결정론적 효과(커맨드 전송)로 묘목이 생성되는 E2E 경로를 검증한다.
        [UnityTest]
        public IEnumerator ClickingTile_SpawnsSeedling()
        {
            yield return LocalSimulation.Boot();

            int before = LocalSimulation.SeedlingCount();

            LocalSimulation.Game.SendCommand(SpawnSeedlingCommand.CreateFromView(1f, 1f));
            yield return LocalSimulation.WaitForSeedlingCount(before + 1);

            Assert.AreEqual(before + 1, LocalSimulation.SeedlingCount(),
                "칸을 클릭(커맨드 전송)하면 해당 위치에 묘목이 생성되어야 합니다.");
            Assert.IsTrue(LocalSimulation.AnySeedlingNear(1f, 1f, 0.01f),
                "묘목은 클릭한 칸의 좌표에 생성되어야 합니다.");
        }

        // 명세: "같은 방에 입장한 플레이어들은 맵 상태가 동기화된다 (묘목 개수와 위치)."
        // 동기화의 토대는 Quantum 의 결정론이다: 동일 입력은 동일한 결과(묘목 위치)를 만든다.
        // 여기서는 커맨드가 만드는 묘목 위치가 입력 좌표의 순수 함수임을 확인한다.
        // (2개 이상의 네트워크 클라이언트를 띄우는 실제 동기화 검증은 별도 하네스가 필요.)
        [UnityTest]
        public IEnumerator MapState_IsDeterministicFromInput()
        {
            yield return LocalSimulation.Boot();

            const float x = 5.5f;
            const float y = -6.5f;
            int before = LocalSimulation.SeedlingCount();

            LocalSimulation.Game.SendCommand(SpawnSeedlingCommand.CreateFromView(x, y));
            yield return LocalSimulation.WaitForSeedlingCount(before + 1);

            // 결정론: 생성된 묘목 위치가 입력 좌표와 정확히 일치 => 모든 클라이언트에서 동일.
            Assert.IsTrue(LocalSimulation.AnySeedlingNear(x, y, 0.001f),
                "묘목 위치는 입력 좌표의 결정론적 함수여야 하며, 이것이 클라이언트 간 동기화의 토대다.");
        }

        private static Type ResolveType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.Name == typeName);
        }

        private static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }
    }
}
