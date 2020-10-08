using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CustomRenderPipeline
{
    public class PostProcessing
    {
        static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int cameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");

        static Mesh fullscreenMesh;
        static Mesh FullscreenMesh
        {
            get
            {
                if (fullscreenMesh != null)
                {
                    return fullscreenMesh;
                }

                fullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                fullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                fullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(1.0f, 1.0f)
                });

                fullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                fullscreenMesh.UploadMeshData(true);
                return fullscreenMesh;
            }
        }

        public static void Render(CommandBuffer buffer, Camera camera, PostProcessingSettings postProcessingSettings, int cameraColorId, int cameraDepthId)
        {
            buffer.SetGlobalTexture(cameraColorTextureId, cameraColorId);
            buffer.SetGlobalTexture(cameraDepthTextureId, cameraDepthId);
            // buffer.Blit(cameraColorId, BuiltinRenderTextureType.CameraTarget, postProcessingSettings.material);

            buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

            // // TODO: clear?
            // // ClearFlag.None,
            // //     Color.clear

            // // buffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            // buffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            // buffer.SetViewport(camera.pixelRect);
            // buffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, postProcessingSettings.material);

            // buffer.SetGlobalMatrix("unity_MatrixVP", GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));
            // buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // buffer.SetViewport(camera.pixelRect);
            buffer.DrawMesh(FullscreenMesh, Matrix4x4.identity, postProcessingSettings.material);
            // buffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            // buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            // buffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            // buffer.SetViewport(camera.pixelRect);
            // buffer.DrawMesh(FullscreenMesh, Matrix4x4.identity, postProcessingSettings.material);
            // buffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

            // buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            // buffer.Blit(cameraColorId, BuiltinRenderTextureType.CameraTarget, postProcessingSettings.material, 0);
        }
    }
}
