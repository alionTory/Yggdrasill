using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using NUnit.Framework;
using Yggdrasill.TestHelper.Protocol;

namespace Yggdrasill.Tests.E2E
{
    public class GameE2ETest
    {
        private ImmutableList<ApplicationRunner> _applicationRunners = null!;
        private string _photonAppVersion = ApplicationRunner.GetCustomPhotonAppVersion();

        [OneTimeSetUp]
        public async Task Setup()
        {
            TestContext.Progress.WriteLine("애플리케이션 시작");
            _applicationRunners = await ApplicationRunners.StartRunners(2, photonAppVersion:_photonAppVersion);
        }

        [Test, Order(1)]
        public async Task GameEntranceByQuickPlayButton()
        {
            await _applicationRunners.WhenAll(app => app.Click(GameObjectId.MultiPlayButton));
            await _applicationRunners[0].Click(GameObjectId.AutoMatchingButton);
            await _applicationRunners[0].WaitUntilSceneLoad(SceneId.MultiplayPrototype);
            await _applicationRunners[1].Click(GameObjectId.AutoMatchingButton);
            await _applicationRunners.WhenAll(app => app.WaitUntilSimulationRunning());
        }

        /// <summary>
        /// 다음 명세를 검증한다. <br/>
        /// - "플레이어가 격자(타일맵) 칸을 클릭하면, 해당 칸에 묘목이 생성된다." <br/>
        /// - "같은 방에 입장한 플레이어들은 묘목의 개수와 위치가 동기화된다."
        /// </summary>
        /// <remarks>
        /// <see cref="GameEntranceByQuickPlayButton"/>이 두 클라이언트를 같은 방에 입장시킨 상태를 전제로 한다.
        /// </remarks>
        [Order(2)]
        [TestCase(1, 1, 3, 4)]
        [TestCase(3, 2, 3, 3)]
        public async Task CheckSeedlingSynchronization(int column1, int row1, int column2, int row2)
        {
            var timeout = TimeSpan.FromSeconds(10);

            await _applicationRunners[0].ClickTile(column1, row1);
            await _applicationRunners.AssertThat(app => app.IsSeedlingExistInTileUntilTimeout(column1, row1, timeout),
                Is.True);

            await _applicationRunners[1].ClickTile(column2, row2);
            await _applicationRunners.AssertThat(app => app.IsSeedlingExistInTileUntilTimeout(column2, row2, timeout),
                Is.True);

            // 위치뿐 아니라 개수도 동기화되어야 한다.
            // (예: 클릭한 클라이언트가 서버를 거치지 않고 묘목을 하나 더 만들면 위치는 맞지만 개수가 어긋난다.)
            var seedlingCount1 = await _applicationRunners[0].GetSeedlingCount();
            var seedlingCount2 = await _applicationRunners[1].GetSeedlingCount();
            Assert.That(seedlingCount1, Is.EqualTo(seedlingCount2), "두 클라이언트의 묘목 개수가 다름.");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            if (_applicationRunners != null)
            {
                foreach (var applicationRunner in _applicationRunners)
                    applicationRunner.Dispose();
            }
        }
    }
}