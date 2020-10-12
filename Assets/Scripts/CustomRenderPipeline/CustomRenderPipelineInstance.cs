using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CustomRenderPipelineInstance : RenderPipeline
    {
        static string multipleDepthTexturesId = "_MULTIPLE_DEPTH_TEXTURES";
        static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
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

                // CameraClearFlags clearFlags = camera.clearFlags;
                // CommandBuffer buffer = new CommandBuffer() { name = camera.name };

                // buffer.SetRenderTarget(
                //     BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                //     BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                // );
                // buffer.ClearRenderTarget(
                //     clearFlags <= CameraClearFlags.Depth,
                //     clearFlags == CameraClearFlags.Color,
                //     clearFlags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear,
                //     1.0f
                // );
                // context.ExecuteCommandBuffer(buffer);
                // buffer.Clear();
                // context.Submit();

                // buffer.GetTemporaryRT(cameraColorTextureId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                // buffer.GetTemporaryRT(cameraDepthTextureId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                // buffer.ClearRenderTarget(
                //     clearFlags <= CameraClearFlags.Depth,
                //     clearFlags == CameraClearFlags.Color,
                //     clearFlags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear,
                //     1.0f
                // );
                // context.ExecuteCommandBuffer(buffer);
                // buffer.Clear();
                // context.Submit();

                // if (postProcessingSettings != null)
                // {
                //     if (clearFlags > CameraClearFlags.Color)
                //     {
                //         clearFlags = CameraClearFlags.Color;
                //     }
                // }

                CommandBuffer buffer = new CommandBuffer() { name = camera.name };

                buffer.GetTemporaryRT(cameraColorTextureId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
                buffer.GetTemporaryRT(cameraDepthTextureId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.GetTemporaryRT(cameraDepthTexture1Id, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.GetTemporaryRT(cameraDepthTexture2Id, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                buffer.GetTemporaryRT(cameraDepthTexture3Id, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();

                ClearFlag clearFlag = GetClearFlag(camera.clearFlags);
                if (clearFlag != ClearFlag.None)
                {
                    buffer.SetRenderTarget(
                        cameraColorTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
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
                if (stack != null)
                {
                    CameraRenderer.Render(context, stack.TertiaryCamera, ClearFlag.Depth, cameraColorTextureId, cameraDepthTexture3Id);
                    CameraRenderer.Render(context, stack.SecondaryCamera, ClearFlag.Depth, cameraColorTextureId, cameraDepthTexture2Id);
                }
                CameraRenderer.Render(context, camera, ClearFlag.Depth, cameraColorTextureId, stack != null ? cameraDepthTexture1Id : cameraDepthTextureId);

                context.SetupCameraProperties(camera);
                if (stack != null)
                {
                    PostProcessing.SetMultipleZBufferParams(buffer, camera, stack.SecondaryCamera, stack.TertiaryCamera); // TODO: conditional stack != null
                    buffer.EnableShaderKeyword(multipleDepthTexturesId);
                } else {
                    buffer.DisableShaderKeyword(multipleDepthTexturesId);
                }
                PostProcessing.Render(buffer, cameraColorTextureId, cameraDepthTextureId, BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, postProcessingSettings);
                context.ExecuteCommandBuffer(buffer);
                buffer.Clear();
                context.Submit();

                buffer.ReleaseTemporaryRT(cameraColorTextureId);
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
