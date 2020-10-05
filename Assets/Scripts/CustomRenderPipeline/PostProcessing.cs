using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class PostProcessing
    {
        static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int cameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");

        static Mesh fullscreenMesh = null;
        static Mesh FullscreenMesh
        {
            get
            {
                if (fullscreenMesh != null)
                {
                    return fullscreenMesh;
                }

                float topV = 1.0f;
                float bottomV = 0.0f;

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
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
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

            buffer.Blit(cameraColorId, BuiltinRenderTextureType.CameraTarget, postProcessingSettings.material);

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
