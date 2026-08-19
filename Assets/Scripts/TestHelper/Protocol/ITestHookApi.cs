using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2eTests
{
    public interface ITestHookApi
    {
        /// <summary>
        /// 게임 프로세스 실행 시, 테스트 훅 서버가 열 포트 번호를 지정하는 명령행 인수의 이름.
        /// </summary>
        public const string PortCommandLineArgumentName = "--test-hook-port";
        
        public Task ClickObject(GameObjectId gameObjectId);

        /// <summary>
        /// <paramref name="sceneId"/>에 해당하는 씬이 로드될 때까지 대기한다.
        /// </summary>
        /// <remarks>
        /// 여기서 "씬 로드"의 정의: 씬 내 모든 활성 초기 오브젝트의 Awake와 OnEnable이 완료된 시점.
        /// </remarks>
        public Task WaitUntilSceneLoad(SceneId sceneId, CancellationToken cancellationToken);

        /// <summary>
        /// 격자(타일맵)의 <paramref name="column"/>열 <paramref name="row"/>행 칸의 중앙에 해당하는 화면 좌표를 클릭한다.
        /// </summary>
        /// <param name="column">가장 왼쪽 열의 칸이 1.</param>
        /// <param name="row">가장 아래 행의 칸이 1.</param>
        /// <exception cref="Exception">
        /// 해당 칸의 중앙이 화면 밖에 있어 클릭할 수 없으면 예외 발생.
        /// </exception>
        public Task ClickTile(int column, int row);
        
        /// <summary>
        /// 현재 격자(타일맵)의 <paramref name="column"/>열 <paramref name="row"/>행 칸에 묘목이 존재하는지 확인한다. <br/>
        /// </summary>
        /// <param name="column">가장 왼쪽 열의 칸이 1.</param>
        /// <param name="row">가장 아래 행의 칸이 1.</param>
        /// <returns>
        /// 묘목이 존재하면 true, 존재하지 않으면 false.
        /// </returns>
        public Task<bool> IsSeedlingExistInTile(int column, int row);

        /// <summary>
        /// 격자(타일맵)의 <paramref name="column"/>열 <paramref name="row"/>행 칸에 묘목이 하나 이상 나타날 때까지 매 프레임 확인하며 대기한다.
        /// </summary>
        /// <param name="column">가장 왼쪽 열의 칸이 1.</param>
        /// <param name="row">가장 아래 행의 칸이 1.</param>
        /// <exception cref="OperationCanceledException">
        /// 묘목이 나타나기 전에 <paramref name="cancellationToken"/>이 취소되면 예외 발생.
        /// </exception>
        public Task WaitUntilSeedlingExistInTile(int column, int row, CancellationToken cancellationToken);

        /// <summary>
        /// 현재 이 클라이언트에 존재하는 묘목의 총 개수를 반환한다.
        /// </summary>
        public Task<int> GetSeedlingCount();
    }
}
