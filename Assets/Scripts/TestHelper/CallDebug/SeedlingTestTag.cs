using UnityEngine;
using Yggdrasill.TestHelper.Debug;

namespace Yggdrasill.TestHelper.CallDebug
{
    /// <summary>
    /// 이 스크립트가 부착된 게임 오브젝트는 생성 시 묘목으로서 <see cref="SeedlingRegistryForTest"/>에 등록되어 조회 가능해진다.
    /// </summary>
    /// <remarks>
    /// 사용법: 묘목 뷰 프리팹의 루트에 부착할 것.
    /// </remarks>
    public class SeedlingTestTag : MonoBehaviour
    {
#if DEBUG
        void OnEnable() => SeedlingRegistryForTest.Register(transform);
        void OnDisable() => SeedlingRegistryForTest.Unregister(transform);
#endif
    }
}
