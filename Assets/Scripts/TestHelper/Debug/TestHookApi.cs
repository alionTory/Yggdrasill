using System;
using System.Threading;
using System.Threading.Tasks;
using Quantum;
using Quantum.Menu;
using UnityEngine;
using Yggdrasill.TestHooks.ClickPoints;
using TMPro;
using UnityEngine.UIElements;
using Yggdrasill.TestHooks.Protocol;
using Yggdrasill.Utilities;

namespace Yggdrasill.TestHooks
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

        public virtual async Task InputToTextField(GameObjectId textFieldId, string text)
        {
            var gameObject = GameObjectRegistryForTest.Get(textFieldId);
            if(gameObject.TryGetComponent<TMP_InputField>(out var inputField))
            {
                await VirtualDevice.InputToTextField(inputField, text);
            }
            else
            {
                throw new Exception($"{textFieldId}에 TMP_InputField 컴포넌트가 없음.");
            }
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

        public virtual async Task WaitUntilSimulationRunning(CancellationToken cancellationToken)
        {
            var frameAdapter = new FrameAdapter();
            var isSimulationRunning = false;
            while (true)
            {
                var frame = QuantumRunner.Default?.Game?.Frames?.Verified;
                if (frame != null)
                {
                    frameAdapter.SetFrame(frame);
                    if (frameAdapter.GameState == GameState.Running)
                        isSimulationRunning = true;
                }
                
                if (isSimulationRunning) break;
                
                await Awaitable.NextFrameAsync(cancellationToken);
            }
        }

        public Task<string> GetInvitationCode()
        {
            var connection = UnityEngine.Object.FindAnyObjectByType<QuantumMenuConnectionBehaviour>();
            if (connection == null)
                return Task.FromException<string>(
                    new Exception($"{nameof(QuantumMenuConnectionBehaviour)} 오브젝트가 씬에 없음."));
            else if (string.IsNullOrEmpty(connection.SessionName))
                return Task.FromException<string>(new Exception(
                    $"{nameof(QuantumMenuConnectionBehaviour)} 오브젝트의 {nameof(connection.SessionName)}이 null 또는 빈 문자열임."));
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