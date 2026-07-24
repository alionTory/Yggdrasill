using System;
using UnityEngine;
using Tests.E2eTests.ClickPointProvider;

namespace Tests.E2eTests
{
    public class TestHookApi
    {
        public async Awaitable ClickObject(GameObjectId gameObjectId)
        {
            var gameObject = GameObjectRegistryForTest.Get(gameObjectId);
            var clickPointProvider = gameObject.GetComponent<IClickPointProvider>();
            if(clickPointProvider == null)
                throw new Exception($"{gameObjectId}에 IClickPointProvider가 없음.");
            
            var clickPoint = clickPointProvider.GetScreenPoint();
            await VirtualDevice.ClickAt(clickPoint);
        }

        
    }
}