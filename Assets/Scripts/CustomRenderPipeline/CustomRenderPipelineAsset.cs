using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        bool usePostProcessing = false;

        protected override RenderPipeline CreatePipeline()
        {
            // Instantiate the Render Pipeline that this custom SRP uses for rendering.
            return new CustomRenderPipelineInstance(usePostProcessing);
        }
    }
}
