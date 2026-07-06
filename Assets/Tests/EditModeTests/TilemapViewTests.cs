using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yggdrasill.Tests.EditMode
{
    /// <summary>
    /// TilemapView 에 대한 단위 테스트.
    ///
    /// 검증 기준은 TilemapView.cs 의 OnPointerClick 주석(명세)이다.
    ///   "(마우스 클릭일 경우) 왼쪽 클릭 이벤트 시에만 실행됨. 중앙, 오른쪽 클릭은 무시함."
    ///
    /// TilemapView 는 Assembly-CSharp 에 속해 asmdef 테스트 어셈블리에서
    /// 컴파일 타임으로 참조할 수 없다. 따라서 리플렉션으로 타입을 얻고,
    /// UnityEngine.EventSystems.IPointerClickHandler 인터페이스를 통해 호출한다.
    ///
    /// 관찰 가능한 계약:
    /// - 중앙/오른쪽 클릭 -> guard 에서 즉시 반환하므로 _tilemap 이나
    ///   QuantumRunner 에 접근하지 않아 예외가 발생하지 않는다.
    /// - 왼쪽 클릭 -> guard 를 통과하여 (미할당) _tilemap.WorldToCell 에 접근하므로
    ///   NullReferenceException 이 발생한다. 즉, 왼쪽 클릭만 실제로 처리된다.
    /// </summary>
    public class TilemapViewTests
    {
        private GameObject _go;
        private IPointerClickHandler _view;

        [SetUp]
        public void SetUp()
        {
            var viewType = ResolveType("TilemapView");
            Assert.IsNotNull(viewType, "TilemapView 타입을 찾을 수 없습니다.");

            _go = new GameObject("TilemapViewUnderTest");
            // [RequireComponent(typeof(TilemapCollider2D))] 로 인해 의존 컴포넌트가 함께 추가된다.
            var component = _go.AddComponent(viewType);
            _view = component as IPointerClickHandler;
            Assert.IsNotNull(_view, "TilemapView 는 IPointerClickHandler 를 구현해야 합니다.");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void OnPointerClick_RightButton_IsIgnored()
        {
            // 오른쪽 클릭은 무시되어야 한다 => guard 에서 반환 => 예외 없음.
            var eventData = MakePointerEvent(PointerEventData.InputButton.Right);

            Assert.DoesNotThrow(() => _view.OnPointerClick(eventData));
        }

        [Test]
        public void OnPointerClick_MiddleButton_IsIgnored()
        {
            // 중앙 클릭은 무시되어야 한다 => guard 에서 반환 => 예외 없음.
            var eventData = MakePointerEvent(PointerEventData.InputButton.Middle);

            Assert.DoesNotThrow(() => _view.OnPointerClick(eventData));
        }

        [Test]
        public void OnPointerClick_LeftButton_IsProcessed()
        {
            // 왼쪽 클릭만 실제로 처리된다. _tilemap 이 미할당(null)이므로
            // guard 를 통과했음을 증명하는 NullReferenceException 이 발생한다.
            var eventData = MakePointerEvent(PointerEventData.InputButton.Left);

            Assert.Throws<NullReferenceException>(() => _view.OnPointerClick(eventData));
        }

        private static PointerEventData MakePointerEvent(PointerEventData.InputButton button)
        {
            return new PointerEventData(EventSystem.current)
            {
                button = button
            };
        }

        private static Type ResolveType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.Name == typeName);
        }

        private static Type[] SafeGetTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }
    }
}
