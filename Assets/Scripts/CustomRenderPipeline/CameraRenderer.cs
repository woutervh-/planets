using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CameraRenderer
    {
        static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        public static void Render(ScriptableRenderContext context, Camera camera, ClearFlag clearFlag, RenderTargetIdentifier targetColor, RenderTargetIdentifier targetDepth)
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

            CommandBuffer buffer = new CommandBuffer() { name = camera.name };

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            Lighting.SetupLights(buffer, visibleLights, cullingResults);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            context.SetupCameraProperties(camera);

            SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                perObjectData = PerObjectData.LightData | PerObjectData.LightIndices
            };
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);

            buffer.SetRenderTarget(
                targetColor, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                targetDepth, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            if (clearFlag != ClearFlag.None)
            {
                buffer.ClearRenderTarget(
                    (clearFlag & ClearFlag.Depth) != 0,
                    (clearFlag & ClearFlag.Color) != 0,
                    (clearFlag & ClearFlag.Color) != 0 ? camera.backgroundColor : Color.clear,
                    1.0f
                );
            }
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            context.DrawSkybox(camera);

#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }
#endif

            // if (postProcessingSettings != null)
            // {
            //     CommandBuffer postProcessingBuffer = new CommandBuffer() { name = "Post-processing" };
            //     postProcessingBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            //     PostProcessing.Render(postProcessingBuffer, camera, postProcessingSettings);
            //     context.ExecuteCommandBuffer(postProcessingBuffer);
            //     postProcessingBuffer.Release();
            // }

#if UNITY_EDITOR
            if (Handles.ShouldRenderGizmos())
            {
                context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
#endif

            buffer.Release();
            context.Submit(); // TODO: necessary?
        }
    }
}
