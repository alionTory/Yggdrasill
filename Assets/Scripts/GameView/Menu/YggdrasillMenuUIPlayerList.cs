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

        private void Reset()
        {
            // 단일 오브젝트를 넘어서는 직렬화 필드 참조는 OnValidate 호출 시점에 로드되지 않을 수 있음.
            // 따라서 OnValidate 대신 Reset에서 필드를 채우고 검증 로그 출력.
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
        [SerializeField] private TMP_Text playersText = null!;

        /// <summary>
        /// The current player count.
        /// </summary>
        [SerializeField] private TMP_Text playersCountText = null!;

        /// <summary>
        /// The max player count.
        /// </summary>
        [SerializeField] private TMP_Text playersMaxCountText = null!;

        public void SetText(string players, string playersCount, string playersMaxCount)
        {
            playersText.text = players;
            playersCountText.text = playersCount;
            playersMaxCountText.text = playersMaxCount;
        }
    }
}