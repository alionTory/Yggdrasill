using System;
using Photon.Deterministic;
using Quantum;

namespace Yggdrasill.Tests.EditMode
{
    public class LocalSimulationRunner
    {
        /// <summary>
        /// 시뮬레이션 시작
        /// </summary>
        public LocalSimulationRunner()
        {

        }

        /// <summary>
        /// 시뮬레이션 종료.
        /// </summary>
        public void ShutDown()
        {
            
        }

        /// <summary>
        /// 로컬 플레이어 추가
        /// </summary>
        public void AddPlayer(Int32 playerIndex)
        {
        }
        
        /// <summary>
        /// <see cref="playerIndex"/>에 해당하는 플레이어가 <see cref="command"/>를 보내도록 함.
        /// </summary>
        public void SendCommand(DeterministicCommand command, Int32 playerIndex)
        {
        }

        public Frame VerifiedFrame;


    }
}