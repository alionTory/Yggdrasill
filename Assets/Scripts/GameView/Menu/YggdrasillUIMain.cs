using System;
using System.Collections.Generic;
using Quantum.Menu;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    [RequireComponent(typeof(YggdrasillSingleplayRunner))]
    public class YggdrasillUIMain : QuantumMenuUIScreen, IValidatable
    {
        [SerializeField] private YggdrasillSingleplayRunner singlePlayRunner = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(singlePlayRunner, result);
            return result;
        }

        private void OnValidate()
        {
            if (singlePlayRunner == null) TryGetComponent(out singlePlayRunner);
            this.LogError();
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
                Controller.Show<YggdrasillUISinglePlay>();
                var connectionResult = await singlePlayRunner.StartLocalAsync(ConnectionArgs);
                await Controller.HandleConnectionResult(connectionResult, Controller);
            }
            catch (Exception ex)
            {
                Debug.LogError($"싱글 플레이 실행 중 오류 발생: {ex}", this);
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