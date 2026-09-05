using System.Collections.Generic;
using Quantum;
using Quantum.Menu;
using UnityEngine;
using UnityEngine.UI;
using Yggdrasill.Utilities;

namespace Yggdrasill.GameView.Menu
{
    public class YggdrasillMenuUISettings : QuantumMenuUISettings, IValidatable
    {
        /// <summary>
        /// 화면을 여는 동안 UI 이벤트로 인한 저장을 막는 플래그.
        /// </summary>
        private bool _suppressSaveChanges;

        /// <summary>
        /// The fullscreen off toggle.
        /// </summary>
        [InlineHelp, SerializeField] protected Toggle _uiFullscreenOff = null!;

        /// <summary>
        /// The VSync off toggle.
        /// </summary>
        [InlineHelp, SerializeField] protected Toggle _uiVSyncCountOff = null!;

        public List<string> Validate()
        {
            var result = new List<string>();
            this.CheckNotNullIfInScene(_uiFullscreenOff, result);
            this.CheckNotNullIfInScene(_uiVSyncCountOff, result);
            return result;
        }


        public override void Awake()
        {
            base.Awake();

            // 선택 가능한 앱 버전 목록이 비어 있으면 QuantumMenuSettingsEntry<string>.Value가 null을 반환하고,
            // SaveChanges()가 ConnectionArgs.AppVersion을 null로 덮어써 다른 클라이언트와 매칭이 안 되게 된다.
            // ConnectionArgs의 앱 버전을 유일한 항목으로 넣어 두면 덮어써도 값이 유지된다.
            _appVersions.Add(ConnectionArgs.AppVersion);
        }

        public override void Show()
        {
            _suppressSaveChanges = true;
            try
            {
                base.Show();
                SetToggle(_uiFullscreen, _uiFullscreenOff, _graphicsSettings.Fullscreen);
                SetToggle(_uiVSyncCount, _uiVSyncCountOff, _graphicsSettings.VSync);
            }
            finally
            {
                _suppressSaveChanges = false;
            }
        }
        
        private static void SetToggle(Toggle onToggle, Toggle offToggle, bool value)
        {
            if(value)
                onToggle.isOn = true;
            else
                offToggle.isOn = true;
        }

        /// <summary>
        /// 화면을 여는 도중 발생한 UI 이벤트는 사용자의 조작이 아니므로 저장하지 않는다.
        /// </summary>
        protected override void SaveChanges()
        {
            if (_suppressSaveChanges) return;
            base.SaveChanges();
        }
    }
}