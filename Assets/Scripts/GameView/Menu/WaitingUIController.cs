using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    /// <summary>
    /// 시뮬레이션 상태가 "대기 중"이면 <see cref="waitingUI"/> 게임 오브젝트를 활성화하고, 그렇지 않으면 비활성화한다.
    /// </summary>
    public class WaitingUIController : QuantumSceneViewComponent, IValidatable
    {
        public GameObject waitingUI = null!;

        private FrameAdapter _frameAdapter = new();

        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(waitingUI, result);
            return result;
        }

        public override void OnInitialize()
        {
            QuantumEvent.Subscribe<EventGameStateChanged>(this, Toggle, onlyIfActiveAndEnabled: true);
        }

        public override void OnActivate(Frame frame)
        {
            _frameAdapter.SetFrame(frame);
            ApplyGameStateToUI(_frameAdapter.GameState);
        }

        public void Toggle(EventGameStateChanged eventGameStateChanged)
        {
            ApplyGameStateToUI(eventGameStateChanged.NewState);
        }

        private void ApplyGameStateToUI(GameState newState)
        {
            if (newState == GameState.Pending)
                waitingUI.SetActive(true);
            else if (newState == GameState.Running)
                waitingUI.SetActive(false);
        }
    }
}