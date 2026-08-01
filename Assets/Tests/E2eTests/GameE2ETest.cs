using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests.E2eTests
{
    public class GameE2ETest
    {
        private Task _oneTimeInitialization = null!;
        private ApplicationRunner _applicationRunner1 = null!;
        private ApplicationRunner _applicationRunner2 = null!;

        [OneTimeSetUp]
        public void Setup()
        {
            _oneTimeInitialization = OneTimeInitialize();
        }

        private async Task OneTimeInitialize()
        {
            TestContext.WriteLine("애플리케이션 시작");
            var applicationRunner1 = ApplicationRunner.StartAsync();
            var applicationRunner2 = ApplicationRunner.StartAsync();
            _applicationRunner1 = await applicationRunner1;
            _applicationRunner2 = await applicationRunner2;
        }

        [Test, Order(1)]
        public async Task GameEntranceByQuickPlayButton()
        {
            await _oneTimeInitialization;
            var timeout = TimeSpan.FromSeconds(10);
            
            await _applicationRunner1.ClickQuickPlayButton();
            try
            {
                await _applicationRunner1.WaitUntilGameEntrance(timeout);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("타임아웃. 게임 입장 실패." + ex.Message);
            }

            await _applicationRunner2.ClickQuickPlayButton();
            try
            {
                await _applicationRunner2.WaitUntilGameEntrance(timeout);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("타임아웃. 게임 입장 실패." + ex.Message);
            }
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
        [TestCase(1,1, 3, 4)]
        [TestCase(3,2, 3, 3)]
        public async Task CheckSeedlingSynchronization(int column1, int row1, int column2, int row2)
        {
            await _oneTimeInitialization;
            var timeout = TimeSpan.FromSeconds(1);

            await _applicationRunner1.ClickTile(column1, row1);
            Assert.That(await _applicationRunner1.IsSeedlingExistInTile(column1, row1, timeout), Is.True);
            Assert.That(await _applicationRunner2.IsSeedlingExistInTile(column1, row1, timeout), Is.True);

            await _applicationRunner2.ClickTile(column2, row2);
            Assert.That(await _applicationRunner1.IsSeedlingExistInTile(column2, row2, timeout), Is.True);
            Assert.That(await _applicationRunner2.IsSeedlingExistInTile(column2, row2, timeout), Is.True);

            // 위치뿐 아니라 개수도 동기화되어야 한다.
            // (예: 클릭한 클라이언트가 서버를 거치지 않고 묘목을 하나 더 만들면 위치는 맞지만 개수가 어긋난다.)
            var seedlingCount1 = await _applicationRunner1.GetSeedlingCount();
            var seedlingCount2 = await _applicationRunner2.GetSeedlingCount();
            Assert.That(seedlingCount1, Is.EqualTo(seedlingCount2), "두 클라이언트의 묘목 개수가 다름.");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _oneTimeInitialization?.ContinueWith(t =>
            {
                _applicationRunner1?.Dispose();
                _applicationRunner2?.Dispose();
            });
        }
    }
}