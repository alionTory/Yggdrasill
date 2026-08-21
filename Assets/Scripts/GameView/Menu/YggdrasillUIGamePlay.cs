using System.Threading.Tasks;
using Quantum.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumUser.View.Menu
{
    public class YggdrasillUIGamePlay:YggdrasillMenuUIScreen
    {
        [SerializeField] private Text invitationCodeText = null!;

        public override void Show()
        {
            Contract.RequireNotNull(ConnectionManager);
            base.Show();
            invitationCodeText.text = ConnectionManager.InvitationCode ?? string.Empty;
        }

        public virtual async Task OnDisconnectPressed()
        {
            Contract.RequireNotNull(ConnectionManager);
            await ConnectionManager.DisconnectAsync(ConnectFailReason.UserRequest);
            Controller.Show<YggdrasillUIMain>();
        } 

    }
}