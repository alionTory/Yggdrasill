using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Tests.E2eTests;

public class SinglePlayEntranceTest
{
    private ApplicationRunner _applicationRunner = null!;

    [SetUp]
    public async Task Setup()
    {
        _applicationRunner = await ApplicationRunner.StartAsync();
    }

    /// <summary>
    /// 싱글 플레이 진입 및 묘목 설치 테스트
    /// </summary>
    [TestCase(1,1)]
    [TestCase(2,3)]
    public async Task SinglePlayEntrance(int tileColumn, int tileRow)
    {
        await _applicationRunner.Click(GameObjectId.SinglePlayButton);
        await _applicationRunner.WaitUntilSimulationRunning();
        await _applicationRunner.ClickTile(tileColumn, tileRow);
        await _applicationRunner.IsSeedlingExistInTileUntilTimeout(tileColumn, tileRow);
    }

    [TearDown]
    public void TearDown()
    {
        _applicationRunner?.Dispose();
    }
    
}