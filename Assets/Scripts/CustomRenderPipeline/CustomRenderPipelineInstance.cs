using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CustomRenderPipelineInstance : RenderPipeline
    {
        bool usePostProcessing;

        public CustomRenderPipelineInstance(bool usePostProcessing)
        {
            this.usePostProcessing = usePostProcessing;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
            {
                bool usePostProcessing = this.usePostProcessing;
                if (camera.cameraType > CameraType.SceneView)
                {
                    usePostProcessing = false;
                }
                if (camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
                {
                    usePostProcessing = false;
                }
                CameraRenderer.Render(context, camera, usePostProcessing);
            }
        }
    }
}
