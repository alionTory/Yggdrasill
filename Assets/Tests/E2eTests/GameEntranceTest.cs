using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Tests.E2eTests
{
    public class GameEntranceTest
    {
        private Task _oneTimeInitialization = null!;
        private ApplicationRunner _applicationRunner1 = null!;

        [OneTimeSetUp]
        public void Setup()
        {
            _oneTimeInitialization = OneTimeInitialize();
        }

        private async Task OneTimeInitialize()
        {
            Debug.Log("애플리케이션 시작");
            _applicationRunner1 = await ApplicationRunner.StartAsync();
        }

        [Test]
        public async Task GameEntranceByQuickPlayButton()
        {
            await _oneTimeInitialization;
            await _applicationRunner1.ClickQuickPlayButton();
            try
            {
                await _applicationRunner1.WaitUntilGameEntrance(TimeSpan.FromSeconds(10));
            }
            catch (OperationCanceledException ex)
            {
                Assert.Fail("타임아웃. 게임 입장 실패." + ex.Message);
            }
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _oneTimeInitialization?.ContinueWith(t => { _applicationRunner1?.Dispose(); });
        }
    }
}