using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
            [CallerArgumentExpression("value")] string? valueName = null) where T : UnityEngine.Object
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

        /// <summary>
        /// <see cref="EditorApplication.delayCall"/>을 사용하여 <see cref="LogError"/>를 지연 호출한다.
        /// </summary>
        /// <remarks>
        /// Unity의 OnValidate 호출 시점에, 단일 게임 오브젝트 내의 직렬화 필드 참조는 정상적으로 로드되어 있지만, 게임 오브젝트를 넘어서는 참조는 아직 제대로 로드되지 않았을 수 있다.
        /// 이로 인해, 직렬화 필드 값이 정상적으로 할당되어 있음에도, OnValidate에서 <see cref="LogError"/>를 호출할 때 필드 값이 null이라며 잘못된 에러 로그가 뜰 수 있다.
        /// 이를 방지하려면, 단일 게임 오브젝트를 넘어서는 대상을 참조하는 직렬화 필드를 검사할 때는 이 메서드를 대신 호출할 것.
        /// </remarks>
        [Conditional("UNITY_EDITOR")]
        public static void LogErrorDelayed<T>(this T obj) where T : Object, IValidatable
        {
#if UNITY_EDITOR
            /*
             * 프리팹 에셋 자체에 대해서는 에러 로그를 출력하지 않아야 함.
             * 유니티는 씬에 배치된 프리팹 뿐만 아니라 프리팹 에셋 자체에 대해서도 OnValidate를 호출하는데,
             * 프리팹 에셋에는 프리팹 외부 컴포넌트에 대한 참조가 존재할 수 없기 때문.
             */
            if (!PrefabUtility.IsPartOfPrefabAsset(obj))
                EditorApplication.delayCall += obj.LogError;
#endif
        }
    }
}