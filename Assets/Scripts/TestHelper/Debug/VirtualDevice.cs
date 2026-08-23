using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.E2eTests
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
        
        public static async Awaitable InputText(string text)
        {
            var keyboard = Keyboard();
            foreach (var c in text)
                InputSystem.QueueTextEvent(keyboard, c);
            await Awaitable.NextFrameAsync();
        }
    }
}