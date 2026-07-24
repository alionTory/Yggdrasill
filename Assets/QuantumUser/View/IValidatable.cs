using System.Collections.Generic;

namespace QuantumUser.View
{
    /// <summary>
    /// MonoBehaviour 스크립트 또는 ScriptableObject의 컴파일 타임 검증 로직을 정의한다.
    /// </summary>
    public interface IValidatable
    {
        
        /// <summary>
        /// 검증을 수행하고, 에러 메시지 리스트를 반환한다.
        /// </summary>
        /// <returns>
        /// 검증 성공 시 빈 리스트 반환.
        /// </returns>
        public List<string> Validate();

        /// <summary>
        /// 검증 성공 여부를 반환한다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 <see cref="Validate"/>를 호출한다.
        /// </remarks>
        public bool IsValid()
        {
            return Validate().Count == 0;
        }
    }
}