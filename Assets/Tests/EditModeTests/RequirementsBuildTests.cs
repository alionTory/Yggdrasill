using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Yggdrasill.Tests.EditMode
{
    /// <summary>
    /// Docs/requirements.adoc 기반 E2E 성격의 구성(설정) 검증.
    ///
    /// 명세: "위그드라실은 메뉴 씬으로 시작한다."
    /// => 빌드 설정의 첫 번째(활성) 씬이 메뉴 씬(MenuPrototype)이어야 한다.
    /// </summary>
    public class RequirementsBuildTests
    {
        /// <summary>
        /// 게임이 "MenuPrototype" 씬으로 시작해야 함.
        /// </summary>
        [Test]
        public void Game_StartsFromMenuScene()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .ToArray();

            Assert.IsNotEmpty(enabledScenes, "빌드 설정에 활성화된 씬이 있어야 합니다.");
            StringAssert.Contains(
                "MenuPrototype",
                enabledScenes[0].path,
                "게임은 메뉴 씬(MenuPrototype)으로 시작해야 합니다.");
        }

        [Test]
        public void GameScene_IsRegisteredInBuild()
        {
            // 명세: 메뉴에서 게임 씬으로 전환된다. => 게임 씬이 빌드에 포함되어야 한다.
            var enabledScenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            Assert.IsTrue(
                enabledScenePaths.Any(p => p.Contains("MultiplayPrototype")),
                "게임 씬(MultiplayPrototype)이 빌드 설정에 포함되어야 합니다.");
        }
    }
}
