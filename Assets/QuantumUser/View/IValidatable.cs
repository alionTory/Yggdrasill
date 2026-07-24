using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

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

        /// <summary>
        /// <see cref="value"/>가 null이면 <see cref="errorMessages"/>에 에러 메시지 문자열을 추가한다.
        /// </summary>
        public static void CheckNotNull<T>(T? value, List<string> errorMessages,
            [CallerArgumentExpression(nameof(value))]
            string? valueName = null) where T : class
        {
            if (value == null)
                errorMessages.Add($"{valueName} is null.");
        }
    }

    public static class ValidateExtensions
    {
        /// <summary>
        /// 검증을 수행하고, 검증에 실패하면 에러 로그를 출력한다.
        /// </summary>
        /// <remarks>
        /// 내부적으로 <see cref="Validate"/>를 호출한다.
        /// </remarks>
        public static void LogError<T>(this T obj) where T : Object, IValidatable
        {
            foreach (var errorMessage in obj.Validate())
            {
                Debug.LogError(errorMessage, obj);
            }
        }
    }
}