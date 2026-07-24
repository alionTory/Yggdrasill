using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tests.E2eTests
{
    public static class VirtualDevice
    {
        private static Mouse? _mouse;
        private static Mouse Ensure() => _mouse ??= InputSystem.AddDevice<Mouse>("TestMouse");

        public static async Awaitable ClickAt(Vector2 screenPos)
        {
            var m = Ensure();

            InputSystem.QueueStateEvent(m, new MouseState { position = screenPos });
            Awaitable.NextFrameAsync(); // 이동 반영

            InputSystem.QueueStateEvent(m, new MouseState { position = screenPos }
                .WithButton(MouseButton.Left));
            Awaitable.NextFrameAsync(); // press 처리 (UI 모듈이 한 프레임 필요)

            InputSystem.QueueStateEvent(m, new MouseState { position = screenPos });
            Awaitable.NextFrameAsync(); // release → click 성립
        }
    }
}