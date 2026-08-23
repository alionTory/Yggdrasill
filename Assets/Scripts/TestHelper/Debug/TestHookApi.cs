using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Tests.E2eTests.ClickPointProvider;
using TMPro;

namespace Tests.E2eTests
{
    public class TestHookApi : ITestHookApi
    {
        public virtual async Task WaitGameObjectLoad(GameObjectId gameObjectId, CancellationToken cancellationToken)
        {
            while (!GameObjectRegistryForTest.TryGet(gameObjectId, out var gameObject))
            {
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        public virtual async Task ClickObject(GameObjectId gameObjectId)
        {
            var gameObject = GameObjectRegistryForTest.Get(gameObjectId);
            var clickPointProvider = gameObject.GetComponent<IClickPointProvider>();
            if (clickPointProvider == null)
                throw new Exception($"{gameObjectId}에 IClickPointProvider가 없음.");

            var clickPoint = clickPointProvider.GetScreenPoint();
            await VirtualDevice.ClickAt(clickPoint);
        }

        public virtual async Task InputText(string text)
        {
            await VirtualDevice.InputText(text);
        }

        public virtual async Task WaitUntilSceneLoad(SceneId sceneId, CancellationToken cancellationToken)
        {
            var scene = SceneList.Get(sceneId).scene;
            bool sceneLoaded = false;
            while (!sceneLoaded)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
                if (scene.TryGetLoadedScene(out var loadedScene))
                {
                    if (loadedScene.isLoaded) sceneLoaded = true;
                }
            }
        }
        
        public Task<string> GetInvitationCode()
        {
            var gameObject = GameObjectRegistryForTest.Get(GameObjectId.InvitationCodeReadField);
            if (gameObject.TryGetComponent(out TMP_Text invitationCodeText))
                return Task.FromResult(invitationCodeText.text);
            else
                return Task.FromException<string>(new Exception("초대 코드 게임 오브젝트에 TMP_Text 컴포넌트가 없음."));
        }

        public virtual async Task ClickTile(int column, int row)
        {
            var tilemapView = GameObjectRegistryForTest.GetTilemapView();
            Vector2 clickPoint = tilemapView.GetTileClickPosition(new Vector2Int(column, row));
            await VirtualDevice.ClickAt(clickPoint);
        }

        public virtual Task<bool> IsSeedlingExistInTile(int column, int row)
        {
            var tilemapView = GameObjectRegistryForTest.GetTilemapView();
            var result = SeedlingRegistryForTest.CountInCell(tilemapView, new Vector2Int(column, row)) > 0;
            return Task.FromResult(result);
        }

        public virtual async Task WaitUntilSeedlingExistInTile(int column, int row, CancellationToken cancellationToken)
        {
            var tilemapView = GameObjectRegistryForTest.GetTilemapView();

            while (SeedlingRegistryForTest.CountInCell(tilemapView, new Vector2Int(column, row)) == 0)
                await Awaitable.NextFrameAsync(cancellationToken);
        }

        public virtual Task<int> GetSeedlingCount()
        {
            return Task.FromResult(SeedlingRegistryForTest.Count);
        }
    }
}