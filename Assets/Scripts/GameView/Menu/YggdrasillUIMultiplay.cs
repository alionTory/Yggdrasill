using System.Threading.Tasks;
using Quantum.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumUser.View.Menu
{
    public class YggdrasillUIMultiplay : YggdrasillMenuUIScreen
    {
        [SerializeField] private InputField invitationCodeField = null!;

        public virtual async Task OnAutoMatchingButtonPressed()
        {
            Controller.Show<QuantumMenuUILoading>();

            ConnectionArgs.Session = null;
            ConnectionArgs.Creating = true;
            var result = await ConnectionManager.StartOnlineAsync(ConnectionArgs);

            await Controller.HandleConnectionResult(result, Controller);
        }

        public virtual async Task OnPrivateRoomCreateButtonPressed()
        {
            var code = Config.CodeGenerator.Create();
            Controller.Show<QuantumMenuUILoading>();
            Controller.Get<QuantumMenuUILoading>().SetStatusText($"참가 코드: {code}");

            ConnectionArgs.Session = code;
            ConnectionArgs.Creating = true;
            var result = await ConnectionManager.StartOnlineAsync(ConnectionArgs);

            await Controller.HandleConnectionResult(result, Controller);
        }

        public virtual async Task OnPrivateRoomParticipateButtonPressed()
        {
            var code = invitationCodeField.text.ToUpperInvariant();
            if (!Config.CodeGenerator.IsValid(code))
            {
                await Controller.PopupAsync("참가 코드 형식이 올바르지 않습니다.", "참가 실패");
            }
            else
            {
                Controller.Show<QuantumMenuUILoading>();

                ConnectionArgs.Session = code;
                ConnectionArgs.Creating = false;
                var result = await ConnectionManager.StartOnlineAsync(ConnectionArgs);

                await Controller.HandleConnectionResult(result, Controller);
            }
        }
    }
}