using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Tests.E2eTests.ClickPointProvider;

namespace Tests.E2eTests
{
    public class TestHookApi:ITestHookApi
    {
        public virtual async Task ClickObject(GameObjectId gameObjectId)
        {
            var gameObject = GameObjectRegistryForTest.Get(gameObjectId);
            var clickPointProvider = gameObject.GetComponent<IClickPointProvider>();
            if (clickPointProvider == null)
                throw new Exception($"{gameObjectId}에 IClickPointProvider가 없음.");

            var clickPoint = clickPointProvider.GetScreenPoint();
            await VirtualDevice.ClickAt(clickPoint);
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
                    if (loadedScene.IsValid()) sceneLoaded = true;
                }
            }
        }

        public virtual async Task ClickTile(int column, int row)
        {
            var tilemapView = GameObjectRegistryForTest.GetTilemapView();
            Vector2 clickPoint = tilemapView.GetTileClickPosition(new Vector2Int(column, row));
            await VirtualDevice.ClickAt(clickPoint);
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
