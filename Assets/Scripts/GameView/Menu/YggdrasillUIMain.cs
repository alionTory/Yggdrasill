using System.Threading.Tasks;
using Quantum.Menu;
using UnityEngine;

namespace QuantumUser.View.Menu
{
    public class YggdrasillUIMain : QuantumMenuUIScreen
    {
        
        /// <summary>
        /// 비-소유 연결 관리 객체
        /// </summary>
        public YggdrasillMenuConnection? ConnectionManager { get; set; }

        public override void Awake()
        {
            base.Awake();
            // 게임 창 포커스가 떠나도 게임이 계속 실행되도록 함.
            if (!Application.runInBackground) Application.runInBackground = true;
        }

        public override void Init()
        {
            base.Init();
            ConnectionArgs.SetDefaults(Config);
        }

        public virtual async Task OnSinglePlayButtonPressed()
        {
            Contract.RequireNotNull(ConnectionManager);
            
            Controller.Show<QuantumMenuUILoading>();
            var connectionResult = await ConnectionManager.StartLocalAsync(ConnectionArgs);
            await Controller.HandleConnectionResult(connectionResult, Controller);
        }
        
        public virtual void OnMultiplayButtonPressed()
        {
            Controller.Show<YggdrasillUIMultiplay>();
        }
    }
}