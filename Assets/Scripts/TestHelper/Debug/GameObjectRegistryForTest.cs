using System;
using System.Collections.Generic;
using UnityEngine;
using Yggdrasill.GameView;
using Yggdrasill.TestHooks.Protocol;

namespace Yggdrasill.TestHooks.Debug
{
    /// <summary>
    /// E2E 테스트를 위해 GameObject를 등록하고 조회할 수 있는 레지스트리이다.
    /// </summary>
    /// <remarks>
    /// 레지스트리 등록 방법은 <see cref="TestId"/> 참고.
    /// </remarks>
    public static class GameObjectRegistryForTest
    {
        private static readonly Dictionary<GameObjectId, GameObject> _map = new();

        /// <summary>
        /// 게임 오브젝트 <see cref="go"/>를 식별자 <see cref="id"/>로 등록한다.
        /// </summary>
        /// <exception cref="Exception"><see cref="id"/>가 이미 등록된 상태면 예외 발생</exception>
        public static void Register(GameObjectId id, GameObject go)
        {
            var addResult = _map.TryAdd(id, go);
            if (!addResult)
                throw new Exception(
                    $"id '{id}' 가 {nameof(GameObjectRegistryForTest)}에 이미 존재하는 시점에 {nameof(Register)}가 호출됨.");
        }

        public static void Unregister(GameObjectId id) => _map.Remove(id);

        /// <summary>
        /// 레지스트리에서 <see cref="id"/>에 대응되는 게임 오브젝트를 조회한다. <br/>
        /// </summary>
        /// <exception cref="Exception">
        /// <see cref="id"/>가 레지스트리에 없으면 예외 발생
        /// </exception>
        public static GameObject Get(GameObjectId id)
        {
            var getResult = _map.TryGetValue(id, out var result);
            if (getResult)
                return result;
            else
                throw new Exception($"id '{id}' 가 {nameof(GameObjectRegistryForTest)}에 존재하지 않지만, {nameof(Get)}이 호출됨.");
        }

        /// <summary>
        /// 레지스트리에서 <see cref="id"/>에 대응되는 게임 오브젝트를 조회한다. <br/>
        /// </summary>
        /// <returns>
        /// 레지스트리에 <see cref="id"/>가 존재하면 true, 존재하지 않으면 false 반환. <br/>
        /// </returns>
        public static bool TryGet(GameObjectId id, out GameObject result)
        {
            return _map.TryGetValue(id, out result);
        }

        /// <summary>
        /// 현재 씬의 격자(타일맵)를 조회한다.
        /// </summary>
        /// <exception cref="Exception">
        /// 격자가 <see cref="GameObjectRegistryForTest"/>에 등록되어 있지 않거나, 등록된 게임 오브젝트에 <see cref="TilemapView"/>컴포넌트가 없으면 예외 발생.
        /// </exception>
        public static TilemapView GetTilemapView()
        {
            var gameObject = Get(GameObjectId.Tilemap);
            if (!gameObject.TryGetComponent<TilemapView>(out var tilemapView))
                throw new Exception($"{GameObjectId.Tilemap}에 {nameof(TilemapView)}가 없음.");
            return tilemapView;
        }
    }
}