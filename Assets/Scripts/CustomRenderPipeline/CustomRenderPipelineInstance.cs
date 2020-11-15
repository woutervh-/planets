using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CustomRenderPipelineInstance : RenderPipeline
    {
        static string multipleDepthTexturesId = "_MULTIPLE_DEPTH_TEXTURES";
        // static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int cameraColorTexture1Id = Shader.PropertyToID("_CameraColorTexture1");
        static int cameraColorTexture2Id = Shader.PropertyToID("_CameraColorTexture2");
        static int cameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        static int cameraDepthTexture1Id = Shader.PropertyToID("_CameraDepthTexture1");
        static int cameraDepthTexture2Id = Shader.PropertyToID("_CameraDepthTexture2");
        static int cameraDepthTexture3Id = Shader.PropertyToID("_CameraDepthTexture3");

        PostProcessingSettings postProcessingSettings;

        public CustomRenderPipelineInstance(PostProcessingSettings postProcessingSettings)
        {
            this.postProcessingSettings = postProcessingSettings;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            {
                CommandBuffer buffer = new CommandBuffer() { name = "Configure" };
                // buffer.GetTemporaryRT(cameraColorTextureId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                buffer.GetTemporaryRT(cameraColorTexture1Id, Camera.main.pixelWidth, Camera.main.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                buffer.GetTemporaryRT(cameraColorTexture2Id, Camera.main.pixelWidth, Camera.main.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                buffer.GetTemporaryRT(cameraDepthTextureId, Camera.main.pixelWidth, Camera.main.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.GetTemporaryRT(cameraDepthTexture1Id, Camera.main.pixelWidth, Camera.main.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.GetTemporaryRT(cameraDepthTexture2Id, Camera.main.pixelWidth, Camera.main.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.GetTemporaryRT(cameraDepthTexture3Id, Camera.main.pixelWidth, Camera.main.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                context.ExecuteCommandBuffer(buffer);
                context.Submit();
                buffer.Release();
            }

            HashSet<Camera> subCameras = new HashSet<Camera>();
            foreach (Camera camera in cameras)
            {
                CameraStack stack = camera.GetComponent<CameraStack>();
                if (stack != null)
                {
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

                CommandBuffer buffer = new CommandBuffer() { name = camera.name };

                ClearFlag clearFlag = GetClearFlag(camera.clearFlags);
                if (clearFlag != ClearFlag.None)
                {
                    buffer.SetRenderTarget(
                        cameraColorTexture1Id, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                        cameraDepthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                    );
                    buffer.ClearRenderTarget(
                        (clearFlag & ClearFlag.Depth) != 0,
                        (clearFlag & ClearFlag.Color) != 0,
                        (clearFlag & ClearFlag.Color) != 0 ? camera.backgroundColor : Color.clear,
                        1.0f
                    );
                }
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();

                CameraStack stack = camera.GetComponent<CameraStack>();
                if (stack != null && stack.enabled)
                {
                    CameraRenderer.Render(context, stack.TertiaryCamera, ClearFlag.None, cameraColorTexture1Id, cameraDepthTexture3Id);
                    CameraRenderer.Render(context, stack.SecondaryCamera, ClearFlag.None, cameraColorTexture1Id, cameraDepthTexture2Id);
                }
                CameraRenderer.Render(context, camera, ClearFlag.Depth, cameraColorTexture1Id, stack != null ? cameraDepthTexture1Id : cameraDepthTextureId);

                context.SetupCameraProperties(camera);
                if (stack != null && stack.enabled)
                {
                    PostProcessing.SetMultipleZBufferParams(buffer, camera, stack.SecondaryCamera, stack.TertiaryCamera);
                    buffer.EnableShaderKeyword(multipleDepthTexturesId);
                }
                else
                {
                    buffer.DisableShaderKeyword(multipleDepthTexturesId);
                }
                PostProcessing.RenderOceanPass(buffer, cameraColorTexture1Id, cameraDepthTextureId, cameraColorTexture2Id, BuiltinRenderTextureType.CameraTarget, postProcessingSettings);
                PostProcessing.RenderAtmospherePass(buffer, cameraColorTexture2Id, cameraDepthTextureId, cameraColorTexture1Id, BuiltinRenderTextureType.CameraTarget, postProcessingSettings);
                PostProcessing.Blit(buffer, cameraColorTexture1Id, cameraDepthTextureId, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(buffer);
                context.Submit();
                buffer.Release();
            }

            {
                CommandBuffer buffer = new CommandBuffer() { name = "Post-processing" };
                CameraStack stack = Camera.main.GetComponent<CameraStack>();
                context.SetupCameraProperties(Camera.main);
                if (stack != null && stack.enabled)
                {
                    PostProcessing.SetMultipleZBufferParams(buffer, Camera.main, stack.SecondaryCamera, stack.TertiaryCamera);
                    buffer.EnableShaderKeyword(multipleDepthTexturesId);
                }
                else
                {
                    buffer.DisableShaderKeyword(multipleDepthTexturesId);
                }
                PostProcessing.RenderOceanPass(buffer, cameraColorTexture1Id, cameraDepthTextureId, cameraColorTexture2Id, BuiltinRenderTextureType.CameraTarget, postProcessingSettings);
                PostProcessing.RenderAtmospherePass(buffer, cameraColorTexture2Id, cameraDepthTextureId, cameraColorTexture1Id, BuiltinRenderTextureType.CameraTarget, postProcessingSettings);
                PostProcessing.Blit(buffer, cameraColorTexture1Id, cameraDepthTextureId, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();
                context.Submit();

                buffer.ReleaseTemporaryRT(cameraColorTexture1Id);
                buffer.ReleaseTemporaryRT(cameraColorTexture2Id);
                buffer.ReleaseTemporaryRT(cameraDepthTextureId);
                buffer.ReleaseTemporaryRT(cameraDepthTexture1Id);
                buffer.ReleaseTemporaryRT(cameraDepthTexture2Id);
                buffer.ReleaseTemporaryRT(cameraDepthTexture3Id);
                context.ExecuteCommandBuffer(buffer);
                buffer.Release();
            }
        }

        static ClearFlag GetClearFlag(CameraClearFlags cameraClearFlags)
        {
            ClearFlag clearFlag = ClearFlag.None;
            if (cameraClearFlags <= CameraClearFlags.Depth)
            {
                clearFlag = clearFlag | ClearFlag.Depth;
            }
            if (cameraClearFlags == CameraClearFlags.Color)
            {
                clearFlag = clearFlag | ClearFlag.Color;
            }
            return clearFlag;
        }
    }
}
