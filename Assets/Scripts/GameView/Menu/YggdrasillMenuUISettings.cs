using Quantum.Menu;

namespace Yggdrasill.GameView.Menu
{
    public class YggdrasillMenuUISettings : QuantumMenuUISettings
    {
        public override void OnBackButtonPressed()
        {
            Controller.Show<YggdrasillUIMain>();
        }
    }
}