using QuantumUser.View;
using UnityEngine;

namespace Editor 
{
    /// <summary>
    /// 프로젝트에서 사용할 씬 정보를 한 곳에 저장하기 위한 에셋.
    /// </summary>
    /// <remarks>
    /// 이 에셋은 프로젝트 내에 정확히 하나 존재해야 한다.
    /// </remarks>
    [CreateAssetMenu(fileName = "SceneList", menuName = "Scriptable Objects/SceneList")]
    public class SceneListConfigSchema : ScriptableObject
    {
        public SceneInfo[] scenes = { }; 

        public bool Validate()
        {
            foreach (var sceneInfo in scenes)
            {
                if (sceneInfo.scene == null)
                {
                    Debug.LogError($"SceneListConfigSchema asset의 유효성 검증 실패: SceneInfo.scene이 null입니다. SceneInfo: {sceneInfo}");
                    return false;
                }
                else if (sceneInfo.hasRuntimeConfig)
                {
                    if (sceneInfo.runtimeConfig == null ||
                        sceneInfo.runtimeConfig.Map == null ||
                        sceneInfo.runtimeConfig.SimulationConfig == null ||
                        sceneInfo.runtimeConfig.SystemsConfig == null)
                    {
                        Debug.LogError($"SceneListConfigSchema asset의 유효성 검증 실패: hasRuntimeConfig가 true이나, runtimeConfig가 제대로 설정되지 않았습니다. SceneInfo: {sceneInfo}");
                        return false;
                    }
                }
            }
            return true;
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}