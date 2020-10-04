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

        public static void Render(ScriptableRenderContext context, Camera camera, bool usePostProcessing)
        {
#if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
#endif

            ScriptableCullingParameters cullingParameters;
            CullingResults cullingResults;
            if (camera.TryGetCullingParameters(out cullingParameters))
            {
                cullingResults = context.Cull(ref cullingParameters);
            }
            else
            {
                return;
            }

            context.SetupCameraProperties(camera);

            CommandBuffer renderBuffer = new CommandBuffer() { name = "Render" };
            if (usePostProcessing)
            {
                renderBuffer.GetTemporaryRT(cameraColorTextureId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Default);
                renderBuffer.SetRenderTarget(cameraColorTextureId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                context.ExecuteCommandBuffer(renderBuffer);
                renderBuffer.Clear();
            }

            CommandBuffer clearBuffer = new CommandBuffer() { name = "Clear" };
            CameraClearFlags clearFlags = camera.clearFlags;
            if (usePostProcessing)
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
            DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
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

            if (usePostProcessing)
            {
                CommandBuffer postProcessingBuffer = new CommandBuffer() { name = "Post-processing" };
                PostProcessing.Render(postProcessingBuffer, cameraColorTextureId);
                context.ExecuteCommandBuffer(postProcessingBuffer);
                postProcessingBuffer.Release();

                renderBuffer.ReleaseTemporaryRT(cameraColorTextureId);
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
