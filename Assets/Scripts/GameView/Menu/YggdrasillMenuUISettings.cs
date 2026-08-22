using Quantum.Menu;

namespace QuantumUser.View.Menu
{
    public class YggdrasillMenuUISettings : QuantumMenuUISettings
    {
        public override void OnBackButtonPressed()
        {
            Controller.Show<YggdrasillUIMain>();
        }
    }
}