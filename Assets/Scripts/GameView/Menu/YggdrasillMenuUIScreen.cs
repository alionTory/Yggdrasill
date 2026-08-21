using Quantum.Menu;

namespace QuantumUser.View.Menu
{
    public class YggdrasillMenuUIScreen:QuantumMenuUIScreen
    {
        /// <summary>
        /// 비-소유 연결 관리 객체
        /// </summary>
        public virtual YggdrasillMenuConnection? ConnectionManager { get; set; }
    }
}