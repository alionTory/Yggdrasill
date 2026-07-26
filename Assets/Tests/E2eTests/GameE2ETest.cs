using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

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
            Debug.Log("애플리케이션 시작");
            var applicationRunner1 = ApplicationRunner.StartAsync();
            var applicationRunner2 = ApplicationRunner.StartAsync();
            _applicationRunner1 = await applicationRunner1;
            _applicationRunner2 = await applicationRunner2;
        }

        [Test, Order(1)]
        public async Task GameEntranceByQuickPlayButton()
        {
            await _oneTimeInitialization;
            await _applicationRunner1.ClickQuickPlayButton();
            await _applicationRunner2.ClickQuickPlayButton();
            try
            {
                var timeout = TimeSpan.FromSeconds(10);
                await _applicationRunner1.WaitUntilGameEntrance(timeout);
                await _applicationRunner2.WaitUntilGameEntrance(timeout);
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("타임아웃. 게임 입장 실패." + ex.Message);
            }
        }

        [TestCase(1,1, 3, 4)]
        [TestCase(3,2, 3, 3)]
        public async Task CheckSeedlingSynchronization(int col1, int row1, int col2, int row2)
        {
            var timeout = TimeSpan.FromSeconds(1);
            
            await _applicationRunner1.ClickTile(col1, row1);
            Assert.That(await _applicationRunner1.IsSeedlingExistInTile(col1, row1, timeout), Is.True);
            Assert.That(await _applicationRunner2.IsSeedlingExistInTile(col1, row1, timeout), Is.True);
            
            await _applicationRunner2.ClickTile(col2, row2);
            Assert.That(await _applicationRunner1.IsSeedlingExistInTile(col2, row2, timeout), Is.True);
            Assert.That(await _applicationRunner2.IsSeedlingExistInTile(col2, row2, timeout), Is.True);
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