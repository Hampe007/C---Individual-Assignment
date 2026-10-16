using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameMenus
{
    public sealed class MainMenuRenderQuality : MonoBehaviour
    {
        [SerializeField] private UniversalRenderPipelineAsset menuPipeline;
        private RenderPipelineAsset previous;

        private void OnEnable()
        {
            if (menuPipeline == null)
            {
                Debug.LogError("MainMenuRenderQuality is missing its menu pipeline asset.", this);
                enabled = false;
                return;
            }
            previous = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = menuPipeline;
        }

        private void OnDisable()
        {
            if (menuPipeline != null && QualitySettings.renderPipeline == menuPipeline)
            {
                QualitySettings.renderPipeline = previous;
            }
        }
    }
}
