using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CustomRenderPipelineInstance : RenderPipeline
    {
        PostProcessingSettings postProcessingSettings;

        public CustomRenderPipelineInstance(PostProcessingSettings postProcessingSettings)
        {
            this.postProcessingSettings = postProcessingSettings;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                PostProcessingSettings postProcessingSettings = this.postProcessingSettings;
#if UNITY_EDITOR
                if (camera.cameraType > CameraType.SceneView)
                {
                    postProcessingSettings = null;
                }
                if (camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
                {
                    postProcessingSettings = null;
                }
#endif
                CameraRenderer.Render(context, camera, postProcessingSettings);
            }
        }
    }
}
