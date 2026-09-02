using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Yggdrasill.TestHelper.Protocol;

namespace Yggdrasill.Tests.E2E;

public class MatchingTest
{
    private string _photonAppVersion = ApplicationRunner.GetCustomPhotonAppVersion();
    private IEnumerable<ApplicationRunner>? _applications = null;
    private static readonly TimeSpan _photonServerTimeout = TimeSpan.FromSeconds(10);

    // 동기화 테스트에 쓰일 타일 번호
    const int tileRow = 1;
    const int tileColumn = 1;

    /// <summary>
    /// Auto Matching 시 클라이언트가 2개씩 짝을 이루어 매칭되어야 한다. <br/>
    /// <paramref name="clientCount"/>가 홀수일 경우, 하나의 클라이언트만 매칭 대기를 유지하고, 나머지는 매칭에 성공해야 한다.
    /// </summary>
    /// <param name="clientCount">매칭을 수행할 클라이언트 수. 1이상이여야 한다.</param>
    [TestCase(3)]
    [TestCase(4)]
    public async Task AutoMatchingTest(int clientCount)
    {
        Log.Write("애플리케이션 시작");
        _applications = await ApplicationRunners.StartRunners(clientCount, photonAppVersion: _photonAppVersion);

        // 멀티플레이 메뉴 진입 및 자동 매칭 버튼 클릭
        await _applications.WhenAll(app => app.Click(GameObjectId.MultiPlayButton));
        foreach (var application in _applications)
        {
            await application.Click(GameObjectId.AutoMatchingButton);
            await application.WaitUntilSceneLoad(SceneId.MultiplayPrototype);
        }

        // 매칭 여부 검증 - 게임 씬 입장 여부 확인
        ApplicationRunner? notMatchedClient = null;
        List<ApplicationRunner> matchedClients = new List<ApplicationRunner>();
        foreach (var application in _applications)
        {
            try
            {
                await application.WaitUntilSimulationRunning();
                matchedClients.Add(application);
            }
            catch (OperationCanceledException ex)
            {
                if (notMatchedClient == null)
                    notMatchedClient = application;
                else
                    Assert.Fail("매칭에 실패한 클라이언트가 한 개 이하여야 함.");
            }
        }

        if (clientCount % 2 == 1)
            Assert.That(notMatchedClient, Is.Not.Null, "홀수 개의 클라이언트 중 매칭에 실패한 클라이언트가 하나 있어야 함.");
        else
            Assert.That(notMatchedClient, Is.Null, "짝수 개의 클라이언트 중 매칭에 실패한 클라이언트가 없어야 함.");

        await matchedClients.WhenAll(async app => Assert.That(
            await app.IsSeedlingExistInTile(tileColumn, tileRow), Is.False,
            $"초기에 타일 ({tileColumn}, {tileRow})에 묘목이 없어야 함.")
        );

        /*
         매칭 여부 검증 - 동기화 체크
         클라이언트 하나가 타일을 클릭하면, 나머지 클라이언트들 중 하나에서 동일한 타일에 묘목이 나타나야 함.
         */
        var syncNotVerifiedClients = matchedClients.ToHashSet();
        while (0 < syncNotVerifiedClients.Count)
        {
            // 클라이언트 하나를 뽑아 타일을 클릭하도록 함.
            var clientToBeCheckedSync = syncNotVerifiedClients.First();
            syncNotVerifiedClients.Remove(clientToBeCheckedSync);
            await clientToBeCheckedSync.ClickTile(tileColumn, tileRow);
            await clientToBeCheckedSync.IsSeedlingExistInTileUntilTimeout(tileColumn, tileRow);

            // 나머지 클라이언트들 중 동기화된 것을 찾음.
            var synchronizedClient = (
                await syncNotVerifiedClients.WhereAsync(app =>
                    app.IsSeedlingExistInTileUntilTimeout(tileColumn, tileRow)
                )
            ).ToArray();

            Assert.That(synchronizedClient.Length, Is.EqualTo(1), "클라이언트는 1:1로 매칭되어 동기화되어야 함.");
            syncNotVerifiedClients.Remove(synchronizedClient[0]);
        }
    }

    [Test]
    public async Task PrivateRoomMatchingTest()
    {
        Log.Write("애플리케이션 시작");
        var twoApplications = await ApplicationRunners.StartRunners(2);
        this._applications = twoApplications;

        // 멀티플레이 메뉴 진입
        await twoApplications.WhenAll(app => app.Click(GameObjectId.MultiPlayButton));

        await twoApplications[0].Click(GameObjectId.PrivateRoomCreateButton);
        await twoApplications[0].WaitUntilSceneLoad(SceneId.MultiplayPrototype);
        var invitationCode = await twoApplications[0].GetInvitationCode();
        Log.Write($"초대 코드: {invitationCode}");

        await twoApplications[1].Click(GameObjectId.InvitationCodeInputField);
        await twoApplications[1].InputToTextField(GameObjectId.InvitationCodeInputField, invitationCode);
        await twoApplications[1].Click(GameObjectId.PrivateRoomParticipateButton);

        // 매칭 여부 검증 - 게임 시뮬레이션 시작 여부 확인
        await twoApplications.WhenAll(app => app.WaitUntilSimulationRunning());

        // 매칭 여부 검증 - 동기화 체크
        await twoApplications.WhenAll(async app =>
            Assert.That(await app.IsSeedlingExistInTile(tileColumn, tileRow), Is.False,
                $"초기에 타일 ({tileColumn}, {tileRow})에 묘목이 없어야 함.")
        );

        await twoApplications[0].ClickTile(tileColumn, tileRow);
        await twoApplications.WhenAll(async app =>
            Assert.That(
                await app.IsSeedlingExistInTileUntilTimeout(tileColumn, tileRow),
                Is.True, "묘목이 동기화되어야 함.")
        );
    }

    [TearDown]
    public void TearDown()
    {
        if (_applications != null)
        {
            foreach (var application in _applications)
            {
                application.Dispose();
            }
        }
    }
}