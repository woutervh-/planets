using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CustomRenderPipelineInstance : RenderPipeline
    {
        static int cameraStackColorTextureId = Shader.PropertyToID("_CameraStackColorTexture");
        static int cameraStackDepthTextureId = Shader.PropertyToID("_CameraStackDepthTexture");

        PostProcessingSettings postProcessingSettings;

        public CustomRenderPipelineInstance(PostProcessingSettings postProcessingSettings)
        {
            this.postProcessingSettings = postProcessingSettings;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            Dictionary<Camera, Camera[]> cameraStacks = new Dictionary<Camera, Camera[]>();
            HashSet<Camera> subCameras = new HashSet<Camera>();
            foreach (Camera camera in cameras)
            {
                CameraStack stack = camera.GetComponent<CameraStack>();
                if (stack != null)
                {
                    cameraStacks.Add(camera, new Camera[] { stack.SecondaryCamera, stack.TertiaryCamera });
                    subCameras.Add(stack.SecondaryCamera);
                    subCameras.Add(stack.TertiaryCamera);
                }
            }

            foreach (Camera camera in cameras)
            {
                if (subCameras.Contains(camera))
                {
                    continue;
                }

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
                if (camera.cameraType == CameraType.Game && Camera.main != camera)
                {
                    postProcessingSettings = null;
                }

                if (cameraStacks.ContainsKey(camera))
                {
                    foreach (Camera subCamera in cameraStacks[camera])
                    {
                        CameraRenderer.Render(context, subCamera, null);
                    }
                }

                // CameraRenderer.Render(context, camera, postProcessingSettings);
                CameraRenderer.Render(context, camera, null);
            }
        }
    }
}
