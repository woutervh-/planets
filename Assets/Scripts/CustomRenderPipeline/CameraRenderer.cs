using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CameraRenderer
    {
        static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int cameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");

        public static void Render(ScriptableRenderContext context, Camera camera, PostProcessingSettings postProcessingSettings)
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            ScriptableCullingParameters cullingParameters;
            CullingResults cullingResults;
            if (camera.TryGetCullingParameters(false, out cullingParameters))
            {
                cullingResults = context.Cull(ref cullingParameters);
            }
            else
            {
                return;
            }

            context.SetupCameraProperties(camera);

            CommandBuffer renderBuffer = new CommandBuffer() { name = "Render" };
            if (postProcessingSettings != null)
            {
                renderBuffer.GetTemporaryRT(cameraColorTextureId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
                renderBuffer.GetTemporaryRT(cameraDepthTextureId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);

                renderBuffer.SetRenderTarget(
                    cameraColorTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    cameraDepthTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
                );
                context.ExecuteCommandBuffer(renderBuffer);
                renderBuffer.Clear();
            }

            CommandBuffer clearBuffer = new CommandBuffer() { name = "Clear" };
            CameraClearFlags clearFlags = camera.clearFlags;
            if (postProcessingSettings != null)
            {
                if (clearFlags > CameraClearFlags.Color)
                {
                    clearFlags = CameraClearFlags.Color;
                }
            }
            clearBuffer.ClearRenderTarget(
                clearFlags <= CameraClearFlags.Depth,
                clearFlags == CameraClearFlags.Color,
                clearFlags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear
            );
            context.ExecuteCommandBuffer(clearBuffer);
            clearBuffer.Release();

            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                perObjectData = PerObjectData.LightData | PerObjectData.LightIndices
            };
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            CommandBuffer cameraBuffer = new CommandBuffer() { name = "Camera" };
            Lighting.SetupLights(cameraBuffer, visibleLights, cullingResults);
            context.ExecuteCommandBuffer(cameraBuffer);
            cameraBuffer.Release();

            context.DrawSkybox(camera);
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
#endif

            if (postProcessingSettings != null)
            {
                CommandBuffer postProcessingBuffer = new CommandBuffer() { name = "Post-processing" };
                postProcessingBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                PostProcessing.Render(postProcessingBuffer, camera, postProcessingSettings, cameraColorTextureId, cameraDepthTextureId);
                context.ExecuteCommandBuffer(postProcessingBuffer);
                postProcessingBuffer.Release();

                renderBuffer.ReleaseTemporaryRT(cameraColorTextureId);
                renderBuffer.ReleaseTemporaryRT(cameraDepthTextureId);
                context.ExecuteCommandBuffer(renderBuffer);
            }
            renderBuffer.Release();

#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
#endif

            context.Submit();
        }
    }
}
