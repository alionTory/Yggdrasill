using System;
using System.Threading.Tasks;
using Quantum.Menu;
using TMPro;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    public class YggdrasillUIMultiplay : QuantumMenuUIScreen
    {
        [SerializeField] private TMP_InputField invitationCodeField = null!;
        
        public virtual async void OnAutoMatchingButtonPressed()
        {
            try
            {
                Controller.Show<QuantumMenuUILoading>();

                ConnectionArgs.Session = null;
                ConnectionArgs.Creating = true;
                var result = await Connection.ConnectAsync(ConnectionArgs);

                await Controller.HandleConnectionResult(result, Controller);
            }
            catch (Exception e)
            {
                Debug.LogError($"자동 매칭 실행 중 오류 발생: {e}", this);
            }
        }

        public virtual async void OnPrivateRoomCreateButtonPressed()
        {
            try
            {
                var code = Config.CodeGenerator.Create();
                Controller.Show<QuantumMenuUILoading>();
                Controller.Get<QuantumMenuUILoading>().SetStatusText($"참가 코드: {code}");

                ConnectionArgs.Session = code;
                ConnectionArgs.Creating = true;
                var result = await Connection.ConnectAsync(ConnectionArgs);

                await Controller.HandleConnectionResult(result, Controller);
            }
            catch (Exception e)
            {
                Debug.LogError($"비공개 방 생성 중 오류 발생: {e}", this);
            }
        }

        public virtual async void OnPrivateRoomParticipateButtonPressed()
        {
            try
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
                    var result = await Connection.ConnectAsync(ConnectionArgs);

                    await Controller.HandleConnectionResult(result, Controller);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"비공개 방 참가 중 오류 발생: {e}", this);
            }
        }

        public virtual void OnBackButtonPressed()
        {
            Controller.Show<QuantumMenuUIMain>();
        }
    }
}