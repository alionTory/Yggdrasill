using System.Collections.Generic;
using UnityEngine;
using Yggdrasill.GameView;

namespace Yggdrasill.TestHelper.Debug
{
    /// <summary>
    /// E2E 테스트를 위해 현재 씬에 존재하는 묘목 뷰를 등록하고 조회할 수 있는 레지스트리이다.
    /// </summary>
    /// <remarks>
    /// 레지스트리 등록 방법은 <see cref="SeedlingTestTag"/> 참고.
    /// </remarks>
    public static class SeedlingRegistryForTest
    {
        private static readonly HashSet<Transform> _seedlings = new();

        /// <summary>
        /// 묘목 뷰의 <see cref="Transform"/> <paramref name="seedling"/>을 등록한다.
        /// </summary>
        public static void Register(Transform seedling) => _seedlings.Add(seedling);

        public static void Unregister(Transform seedling) => _seedlings.Remove(seedling);

        /// <summary>
        /// 등록된 전체 묘목의 개수를 반환한다.
        /// </summary>
        public static int Count => _seedlings.Count;

        /// <summary>
        /// <paramref name="tilemapView"/>의 <paramref name="cell"/> 칸 위에 있는 묘목의 개수를 반환한다.
        /// </summary>
        public static int CountInCell(TilemapView tilemapView, Vector2Int cell)
        {
            int result = 0;
            foreach (var seedling in _seedlings)
            {
                var seedlingCell = tilemapView.WorldToCellPosition(seedling.position);
                if (seedlingCell.x == cell.x && seedlingCell.y == cell.y)
                    result++;
            }

            return result;
        }
    }
}
