using UnityEngine;

namespace Tests.E2eTests
{
    /// <summary>
    /// 이 스크립트가 부착된 게임 오브젝트는 생성 시 <see cref="GameObjectRegistryForTest"/>에 등록되어 조회 가능해진다.
    /// </summary>
    /// <remarks>
    /// 사용법: 인스펙터에서 <see cref="id"/> 필드를 중복되지 않는 값으로 설정해 줄 것.
    /// </remarks>
    public class TestId : MonoBehaviour
    {
        public GameObjectId id; // 예: "ready-button"

#if DEBUG
        void OnEnable() => GameObjectRegistryForTest.Register(id, gameObject);
        void OnDisable() => GameObjectRegistryForTest.Unregister(id);
#endif
    }
}