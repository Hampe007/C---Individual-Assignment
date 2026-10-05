using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameMenus
{
    public sealed class MainMenuRenderQuality : MonoBehaviour
    {
        [SerializeField, Range(1, 8)] private int pixelSize = 4;
        private RenderPipelineAsset previous;
        private UniversalRenderPipelineAsset menuPipeline;

        private void Awake()
        {
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline)
            {
                return;
            }
            previous = QualitySettings.renderPipeline;
            menuPipeline = Instantiate(pipeline);
            menuPipeline.name = "MainMenu Render Quality";
            menuPipeline.renderScale = 1f / Mathf.Clamp(pixelSize, 1, 8);
            menuPipeline.upscalingFilter = UpscalingFilterSelection.Point;
            menuPipeline.supportsHDR = true;
            menuPipeline.hdrColorBufferPrecision = HDRColorBufferPrecision._64Bits;
            menuPipeline.msaaSampleCount = 1;
            QualitySettings.renderPipeline = menuPipeline;
        }

        private void OnDestroy()
        {
            if (menuPipeline == null)
            {
                return;
            }
            if (QualitySettings.renderPipeline == menuPipeline)
            {
                QualitySettings.renderPipeline = previous;
            }
            Destroy(menuPipeline);
        }
    }
}
