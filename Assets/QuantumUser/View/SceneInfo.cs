using System;
using Eflatun.SceneReference;
using Quantum;

namespace QuantumUser.View
{
    [Serializable]
    public class SceneInfo
    {
        /// <summary>
        /// 씬에 대한 참조. 필수로 지정되어야 함.
        /// </summary>
        public SceneReference scene = null!;
        
        /// <summary>
        /// runtimeConfig 가 지정되었는지 여부. 즉, Quantum 시뮬레이션 씬인지 여부.
        /// </summary>
        public bool hasRuntimeConfig;
        
        /// <summary>
        /// hadRuntimeConfig가 참이면 필수로 지정되어야 함.
        /// </summary>
        public RuntimeConfig? runtimeConfig;

        /// <summary>
        /// <see cref="quantumMenuSceneInfo"/>가 현재 <see cref="SceneInfo"/> 객체의 상태를 반영하도록 수정.
        /// </summary>
        /// <remarks>
        /// require <see cref="hasRuntimeConfig"/>
        /// </remarks>
        public void SyncQuantumMenuSceneInfo(QuantumMenuSceneInfo quantumMenuSceneInfo)
        {
            quantumMenuSceneInfo.Name = scene.Name;
            quantumMenuSceneInfo.ScenePath = scene.Path;
            quantumMenuSceneInfo.RuntimeConfig = runtimeConfig;
        }
    }
}