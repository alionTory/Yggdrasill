using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Quantum;
using Quantum.Menu;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    [RequireComponent(typeof(YggdrasillMenuUIPlayerList), typeof(YggdrasillSingleplayRunner))]
    public class YggdrasillUISinglePlay : QuantumMenuUIScreen, IValidatable
    {
        [SerializeField] private YggdrasillSingleplayRunner singlePlayRunner = null!;

        /// <summary>
        /// Toggles this camera on/off when entering or leaving the game screen.
        /// </summary>
        [SerializeField, HideInInspector] private Camera menuCamera = null!;

        /// <summary>
        /// 게임 내 플레이어 리스트가 표시되는 GUI.
        /// </summary>
        [SerializeField, InlineHelp] private YggdrasillMenuUIPlayerList playerListUI = null!;

        /// <summary>
        /// In what frequency are the usernames refreshed.
        /// </summary>
        [InlineHelp] public float updateUsernameRateInSeconds = 2;

        /// <summary>
        /// The coroutine is started during Show() and updates the Usernames using this interval <see cref="updateUsernameRateInSeconds"/>.
        /// </summary>
        private Coroutine? _updateUsernamesCoroutine;

        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(menuCamera, result);
            IValidatable.CheckNotNull(playerListUI, result);
            IValidatable.CheckNotNull(singlePlayRunner, result);
            return result;
        }

        private void Reset()
        {
            if (menuCamera == null) menuCamera = FindAnyObjectByType<Camera>();
        }

        private void OnValidate()
        {
            if (playerListUI == null) TryGetComponent(out playerListUI);
            if (singlePlayRunner == null) TryGetComponent(out singlePlayRunner);
            this.LogErrorDelayed();
        }

        public virtual async void OnDisconnectPressed()
        {
            try
            {
                await singlePlayRunner.DisconnectAsync();
                Controller.Show<QuantumMenuUIMain>();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }


        /// <summary>
        /// The screen show method. Calls partial method <see cref="ShowUser"/> to be implemented on the SDK side.
        /// Will check is the session code is compatible with the party code to toggle the session UI part.
        /// </summary>
        public override void Show()
        {
            base.Show();

            menuCamera.enabled = false;

            UpdateUsernames();

            if (updateUsernameRateInSeconds > 0)
            {
                _updateUsernamesCoroutine = StartCoroutine(UpdateUsernamesCoroutine());
            }
        }

        /// <summary>
        /// The screen hide method. Calls partial method <see cref="HideUser"/> to be implemented on the SDK side.
        /// </summary>
        public override void Hide()
        {
            base.Hide();

            menuCamera.enabled = true;

            if (_updateUsernamesCoroutine != null)
            {
                StopCoroutine(_updateUsernamesCoroutine);
                _updateUsernamesCoroutine = null;
            }
        }


        /// <summary>
        /// Update the usernames list. Will cancel itself if UpdateUsernameRateInSeconds less or equal to 0.
        /// </summary>
        /// <returns></returns>
        private IEnumerator UpdateUsernamesCoroutine()
        {
            while (updateUsernameRateInSeconds > 0)
            {
                yield return new WaitForSeconds(updateUsernameRateInSeconds);
                UpdateUsernames();
            }
        }

        /// <summary>
        /// Update the usernames and toggle the UI part on/off depending on the <see cref="QuantumMenuConnectionBehaviour.Usernames"/>
        /// </summary>
        private void UpdateUsernames()
        {
            if (Connection.Usernames != null && Connection.Usernames.Count > 0)
            {
                playerListUI.gameObject.SetActive(true);
                var sBuilder = new StringBuilder();
                var playerCount = 0;
                foreach (var username in Connection.Usernames)
                {
                    sBuilder.AppendLine(username);
                    playerCount += string.IsNullOrEmpty(username) ? 0 : 1;
                }

                playerListUI.SetText(sBuilder.ToString(), $"{playerCount}", $"/{Connection.MaxPlayerCount}");
            }
            else
            {
                playerListUI.gameObject.SetActive(false);
            }
        }
    }
}