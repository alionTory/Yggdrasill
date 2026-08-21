using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quantum.Menu;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    [RequireComponent(typeof(YggdrasillMenuConnection))]
    public class YggdrasillMenuUIController : QuantumMenuUIController, IValidatable
    {
        [SerializeField] private YggdrasillMenuConnection connectionManager = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(connectionManager, result);
            return result;
        }

        private void OnValidate()
        {
            if(connectionManager==null) TryGetComponent(out connectionManager);
            if(Connection==null) Connection = connectionManager;
            this.LogError();
        }

        protected override void Awake()
        {
            _screenLookup = new Dictionary<Type, QuantumMenuUIScreen>();

            foreach (var screen in _screens)
            {
                screen.Config = _config;
                screen.Config.Init();
                screen.Connection = Connection;
                screen.ConnectionArgs = ConnectArgs;
                screen.Controller = this;

                var t = screen.GetType();
                while (true)
                {
                    _screenLookup.Add(t, screen);
                    if (t.BaseType == null || typeof(QuantumMenuUIScreen).IsAssignableFrom(t) == false ||
                        t.BaseType == typeof(QuantumMenuUIScreen))
                    {
                        break;
                    }

                    t = t.BaseType;
                }

                if (screen is YggdrasillMenuUIScreen yggdrasillScreen)
                {
                    yggdrasillScreen.ConnectionManager = connectionManager;
                }

                if (screen is QuantumMenuUIPopup popupHandler)
                {
                    _popupHandler = popupHandler;
                }
            }

            foreach (var screen in _screens)
            {
                screen.Init();
            }
        }

        public override async Task HandleConnectionResult(ConnectResult result, QuantumMenuUIController controller)
        {
            if (result.CustomResultHandling) return;

            if (result.Success)
            {
                Show<YggdrasillUIGamePlay>();
            }
            else if (result.FailReason != ConnectFailReason.ApplicationQuit)
            {
                var popup = PopupAsync(result.DebugMessage, "접속 실패");
                if (result.WaitForCleanup != null) await Task.WhenAll(result.WaitForCleanup, popup);
                else
                    await popup;
                Show<YggdrasillUIMain>();
            }
        }
    }
}