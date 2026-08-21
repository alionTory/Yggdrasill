using System.Threading.Tasks;
using Quantum.Menu;

namespace QuantumUser.View.Menu
{
    public class YggdrasillMenuUIController : QuantumMenuUIController
    {
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
                Show<YggdrasillUIMain>(); // ← 요구사항: OK 누르면 초기 화면
            }
        }
    }
}