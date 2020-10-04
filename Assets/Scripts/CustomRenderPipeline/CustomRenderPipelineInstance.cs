using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class CustomRenderPipelineInstance : RenderPipeline
    {
        static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera camera in cameras)
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
                    continue;
                }

                context.SetupCameraProperties(camera);
                CommandBuffer clearBuffer = new CommandBuffer();
                clearBuffer.ClearRenderTarget(
                    camera.clearFlags <= CameraClearFlags.Depth,
                    camera.clearFlags == CameraClearFlags.Color,
                    camera.clearFlags == CameraClearFlags.Color ? camera.backgroundColor : Color.clear
                );
                context.ExecuteCommandBuffer(clearBuffer);
                clearBuffer.Release();

                SortingSettings sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
                DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);

                NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
                CommandBuffer cameraBuffer = new CommandBuffer();
                Lighting.SetupLights(cameraBuffer, visibleLights, cullingResults);
                context.ExecuteCommandBuffer(cameraBuffer);
                cameraBuffer.Release();

                context.DrawSkybox(camera);
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

#if UNITY_EDITOR
                if (Handles.ShouldRenderGizmos())
                {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                    context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
                }
#endif

                // Tell the Scriptable Render Context to tell the graphics API to perform the scheduled commands.
                context.Submit();
            }
        }
    }
}
