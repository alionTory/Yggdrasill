using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quantum.Menu;
using Yggdrasill.TestHelper.Protocol;
using UnityEngine;
using Yggdrasill.Utilities;

namespace Yggdrasill.GameView.Menu
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
            ConnectionArgs.AppVersion = ResolveAppVersion();
        }

        /// <summary>
        /// 게임 버전을 구한다.
        /// </summary>
        /// <remarks>
        /// 기본적으로 <see cref="Application.version"/>값을 사용한다. <br/>
        /// 단, <see cref="ITestHookApi.PhotonAppVersionCommandLineArgumentName"/> 명령행 인수가 있으면, 해당 인수로 주어진 값을 대신 사용한다.
        /// </remarks>
        private static string ResolveAppVersion()
        {
#if DEBUG
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == ITestHookApi.PhotonAppVersionCommandLineArgumentName)
                    return args[i + 1];
#endif
            return Application.version;
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