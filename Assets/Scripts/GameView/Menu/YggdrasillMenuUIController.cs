using System;
using System.Threading.Tasks;
using Quantum;
using Quantum.Menu;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    public class YggdrasillMenuUIController : QuantumMenuUIController
    {
        protected override void Awake()
        {
            base.Awake();
            // 0번 스크린을 메인으로 설정.
            _screenLookup.Add(typeof(QuantumMenuUIMain), _screens[0]);
        }

        public async Task HandleLocalConnectionResult(ConnectResult result)
        {
            if (result.Success)
            {
                Show<YggdrasillUISinglePlay>();
            }
            else if (result.FailReason != ConnectFailReason.ApplicationQuit)
            {
                await HandleFail(result);
            }
        }

        public override async Task HandleConnectionResult(ConnectResult result, QuantumMenuUIController controller)
        {
            if (result.Success)
            {
                Show<QuantumMenuUIGameplay>();
            }
            else if (result.FailReason != ConnectFailReason.ApplicationQuit)
            {
                await HandleFail(result);
            }
        }

        private async Task HandleFail(ConnectResult result)
        {
            var popup = PopupAsync(result.DebugMessage, "Connection Failed");
            if (result.WaitForCleanup != null)
            {
                await Task.WhenAll(result.WaitForCleanup, popup);
            }
            else
            {
                await popup;
            }

            Show<YggdrasillUIMain>();
        }
    }
}