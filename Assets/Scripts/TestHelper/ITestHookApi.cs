using System.Threading;
using System.Threading.Tasks;
using QuantumUser.View;
using UnityEngine;

namespace Tests.E2eTests
{
    public interface ITestHookApi
    {
        public Task ClickObject(GameObjectId gameObjectId);

        public Task WaitUntilSceneLoad(SceneInfo sceneInfo, CancellationToken cancellationToken);

        /// <summary>
        /// 격자(타일맵)의 <paramref name="cell.x"/>열 <paramref name="cell.y"/>행 칸의 중앙에 해당하는 화면 좌표를 클릭한다.
        /// </summary>
        /// <param name="cell">
        /// 가장 왼쪽 열의 칸은 <paramref name="cell.x"/>==1, 가장 아래 행의 칸은 <paramref name="cell.y"/>==1.
        /// </param>
        /// <exception cref="Exception">
        /// 해당 칸의 중앙이 화면 밖에 있어 클릭할 수 없으면 예외 발생.
        /// </exception>
        public Task ClickTile(Vector2Int cell);

        /// <summary>
        /// 격자(타일맵)의 <paramref name="cell.x"/>열 <paramref name="cell.y"/>행 칸에 묘목이 하나 이상 나타날 때까지 매 프레임 확인하며 대기한다.
        /// </summary>
        /// <param name="cell">
        /// 가장 왼쪽 열의 칸은 <paramref name="cell.x"/>==1, 가장 아래 행의 칸은 <paramref name="cell.y"/>==1.
        /// </param>
        /// <exception cref="OperationCanceledException">
        /// 묘목이 나타나기 전에 <paramref name="cancellationToken"/>이 취소되면 예외 발생.
        /// </exception>
        public Task WaitUntilSeedlingExistInTile(Vector2Int cell, CancellationToken cancellationToken);

        /// <summary>
        /// 현재 이 클라이언트에 존재하는 묘목의 총 개수를 반환한다.
        /// </summary>
        public Task<int> GetSeedlingCount();
    }
}
