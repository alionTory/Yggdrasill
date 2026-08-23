using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quantum.Menu;
using UnityEditor;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    public class YggdrasillUIMain : QuantumMenuUIScreen, IValidatable
    {
        [SerializeField] private YggdrasillSingleplayRunner singlePlayRunner = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            this.CheckNotNullIfInScene(singlePlayRunner, result);
            return result;
        }

        private void OnValidate()
        {
            this.LogErrorDelayed();
        }

        public override void Awake()
        {
            base.Awake();
            // 게임 창 포커스가 떠나도 게임이 계속 실행되도록 함.
            if (!Application.runInBackground) Application.runInBackground = true;
        }

        public override void Init()
        {
            base.Init();
            ConnectionArgs.SetDefaults(Config);
        }

        public virtual async void OnSinglePlayButtonPressed()
        {
            try
            {
                Controller.Show<QuantumMenuUILoading>();
                var connectionResult = await singlePlayRunner.StartLocalAsync(ConnectionArgs);
                await ((YggdrasillMenuUIController)Controller).HandleLocalConnectionResult(connectionResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"싱글 플레이 실행 중 오류 발생: {ex}", this);
            }
        }

        private async Task HandleConnectionResult(ConnectResult result, QuantumMenuUIController controller)
        {
            if (result.CustomResultHandling)
            {
                return;
            }

            if (result.Success)
            {
                controller.Show<YggdrasillUISinglePlay>();
            }
            else if (result.FailReason != ConnectFailReason.ApplicationQuit)
            {
                var popup = controller.PopupAsync(result.DebugMessage, "Connection Failed");
                if (result.WaitForCleanup != null)
                {
                    await Task.WhenAll(result.WaitForCleanup, popup);
                }
                else
                {
                    await popup;
                }

                controller.Show<QuantumMenuUIMain>();
            }
        }


        public virtual void OnMultiplayButtonPressed()
        {
            Controller.Show<YggdrasillUIMultiplay>();
        }

        public virtual void OnSettingsButtonPressed()
        {
            Controller.Show<QuantumMenuUISettings>();
        }

        public virtual void OnQuitButtonPressed()
        {
            Application.Quit();
        }
    }
}