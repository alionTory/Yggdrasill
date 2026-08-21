using System.Collections.Generic;
using QuantumUser.View;
using TMPro;
using UnityEngine;


namespace QuantumUser.View.Menu
{
    /// <summary>
    /// 플레이어 리스트 UI를 관리한다.
    /// </summary>
    /// <remarks>
    /// 현재는 싱글 플레이 UI(<see cref="YggdrasillUISinglePlay"/>) 에서만 사용된다. <br/>
    /// </remarks>
    public class YggdrasillMenuUIPlayerList : MonoBehaviour, IValidatable
    {
        public List<string> Validate()
        {
            var result = new List<string>();
            IValidatable.CheckNotNull(playersText, result);
            IValidatable.CheckNotNull(playersCountText, result);
            IValidatable.CheckNotNull(playersMaxCountText, result);
            return result;
        }

        private void OnValidate()
        {
            if (playersText == null)
                transform.Find("Scroll View/Viewport/Content/PlayerName")?.TryGetComponent(out playersText);
            if (playersCountText == null)
                transform.Find("Scroll View/Background/CurrentPlayerLabel")?.TryGetComponent(out playersCountText);
            if (playersMaxCountText == null)
                transform.Find("Scroll View/Background/MaxPlayerLabel")?.TryGetComponent(out playersMaxCountText);
            this.LogError();
        }

        /// <summary>
        /// The list of players.
        /// </summary>
        [SerializeField, HideInInspector] private TMP_Text playersText = null!;

        /// <summary>
        /// The current player count.
        /// </summary>
        [SerializeField, HideInInspector] private TMP_Text playersCountText = null!;

        /// <summary>
        /// The max player count.
        /// </summary>
        [SerializeField, HideInInspector] private TMP_Text playersMaxCountText = null!;

        public void SetText(string players, string playersCount, string playersMaxCount)
        {
            playersText.text = players;
            playersCountText.text = playersCount;
            playersMaxCountText.text = playersMaxCount;
        }
    }
}