using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Yggdrasill.TestHooks
{
    public static class VirtualDevice
    {
        private static Mouse? _mouse;
        private static Mouse Mouse() => _mouse ??= InputSystem.AddDevice<Mouse>("TestMouse");
        
        private static Keyboard? _keyboard;
        private static Keyboard Keyboard() => _keyboard ??= InputSystem.AddDevice<Keyboard>("TestKeyboard");

        public static async Awaitable ClickAt(Vector2 screenPos)
        {
            var m = Mouse();

            InputSystem.QueueStateEvent(m, new MouseState { position = screenPos });
            await Awaitable.NextFrameAsync(); // 이동 반영

            InputSystem.QueueStateEvent(m, new MouseState { position = screenPos }
                .WithButton(MouseButton.Left));
            await Awaitable.NextFrameAsync(); // press 처리 (UI 모듈이 한 프레임 필요)

            InputSystem.QueueStateEvent(m, new MouseState { position = screenPos });
            await Awaitable.NextFrameAsync(); // release → click 성립
        }
        
        /// <summary>
        /// <paramref name="text"/>를 키보드로 입력한다.
        /// </summary>
        /// <remarks>
        /// UGUI input field 입력 시뮬레이션은 불가. input field에 입력 시에는 <see cref="InputToTextField"/> 사용.
        /// </remarks>
        public static async Awaitable InputText(string text)
        {
            var keyboard = Keyboard();
            foreach (var c in text)
                InputSystem.QueueTextEvent(keyboard, c);
            await Awaitable.NextFrameAsync();
        }

        /// <summary>
        /// TMP_InputField 컴포넌트에 텍스트를 입력한다.
        /// </summary>
        /// <remarks>
        /// UGUI는 InputSystem과 독립적인 경로로 키보드 입력을 처리하므로,
        /// InputField 입력 시뮬레이션 시 <see cref="InputText"/> 대신 이 메서드를 사용해야 한다.
        /// </remarks>
        public static async Awaitable InputToTextField(TMP_InputField inputField, string text)
        {
            foreach (var c in text)
                inputField.ProcessEvent(new Event { type = EventType.KeyDown, character = c, keyCode = KeyCode.None });
            inputField.ForceLabelUpdate();
            await Awaitable.NextFrameAsync();
        }
    }
}