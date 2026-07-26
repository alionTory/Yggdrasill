using System;
using System.Threading;
using System.Threading.Tasks;
using QuantumUser.View;
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

        public virtual async Task WaitUntilSceneLoad(SceneInfo sceneInfo, CancellationToken cancellationToken)
        {
            bool sceneLoaded = false;
            while (!sceneLoaded)
            {
                await Awaitable.NextFrameAsync(cancellationToken);
                if (sceneInfo.scene.TryGetLoadedScene(out var loadedScene))
                {
                    if (loadedScene.IsValid()) sceneLoaded = true;
                }
            }
        }
    }
}