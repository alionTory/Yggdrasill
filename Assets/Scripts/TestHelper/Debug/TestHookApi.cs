using System;
using System.Threading;
using System.Threading.Tasks;
using Quantum.Menu;
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
            var connection = UnityEngine.Object.FindAnyObjectByType<QuantumMenuConnectionBehaviour>();
            if(connection == null)
                return Task.FromException<string>(new Exception($"{nameof(QuantumMenuConnectionBehaviour)} 오브젝트가 씬에 없음."));
            else if(string.IsNullOrEmpty(connection.SessionName))
                return Task.FromException<string>(new Exception($"{nameof(QuantumMenuConnectionBehaviour)} 오브젝트의 {nameof(connection.SessionName)}이 null 또는 빈 문자열임."));
            else
                return Task.FromResult(connection.SessionName);
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