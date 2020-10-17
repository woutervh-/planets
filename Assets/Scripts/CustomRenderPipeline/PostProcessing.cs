using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class PostProcessing
    {
        static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int cameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        static int cameraDepthTexture1Id = Shader.PropertyToID("_CameraDepthTexture1");
        static int cameraDepthTexture2Id = Shader.PropertyToID("_CameraDepthTexture2");
        static int cameraDepthTexture3Id = Shader.PropertyToID("_CameraDepthTexture3");
        static int zBufferParams1Id = Shader.PropertyToID("_ZBufferParams1");
        static int zBufferParams2Id = Shader.PropertyToID("_ZBufferParams2");
        static int zBufferParams3Id = Shader.PropertyToID("_ZBufferParams3");

        static string precomputedKeyword = "_PRECOMPUTED_OPTICAL_DEPTH";
        static int planetCenterId = Shader.PropertyToID("_PlanetCenter");
        static int planetRadiusId = Shader.PropertyToID("_PlanetRadius");
        static int atmosphereRadiusId = Shader.PropertyToID("_AtmosphereRadius");
        static int atmosphereFalloffRayleighId = Shader.PropertyToID("_AtmosphereFalloffRayleigh");
        static int atmosphereFalloffMieId = Shader.PropertyToID("_AtmosphereFalloffMie");
        static int atmosphereWavelengthsRayleighId = Shader.PropertyToID("_AtmosphereWavelengthsRayleigh");
        static int atmosphereWavelengthsMieId = Shader.PropertyToID("_AtmosphereWavelengthsMie");
        static int atmosphereSunIntensityId = Shader.PropertyToID("_AtmosphereSunIntensity");
        static int opticalDepthTextureId = Shader.PropertyToID("_OpticalDepthTexture");

        static Material copyMaterial;
        static Material CopyMaterial
        {
            get
            {
                if (copyMaterial == null)
                {
                    copyMaterial = new Material(Shader.Find("CustomRenderPipeline/Copy"));
                }
                return copyMaterial;
            }
        }

        static Material copyDepthMaterial;
        static Material CopyDepthMaterial
        {
            get
            {
                if (copyDepthMaterial == null)
                {
                    copyDepthMaterial = new Material(Shader.Find("CustomRenderPipeline/CopyDepth"));
                }
                return copyDepthMaterial;
            }
        }

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

        static void DoAtmospherePass(CommandBuffer buffer, PostProcessingEffects.AtmosphereSettings atmosphereSettings)
        {
            if (atmosphereSettings.Material == null)
            {
                return;
            }

            if (atmosphereSettings.Precomputed)
            {
                atmosphereSettings.Material.SetTexture(opticalDepthTextureId, atmosphereSettings.OpticalDepthTexture);
                atmosphereSettings.Material.EnableKeyword(precomputedKeyword);
            }
            else
            {
                atmosphereSettings.Material.DisableKeyword(precomputedKeyword);
            }

            atmosphereSettings.Material.SetVector(planetCenterId, atmosphereSettings.PlanetCenter);
            atmosphereSettings.Material.SetFloat(planetRadiusId, atmosphereSettings.PlanetRadius);
            atmosphereSettings.Material.SetFloat(atmosphereRadiusId, atmosphereSettings.AtmosphereRadius);
            atmosphereSettings.Material.SetFloat(atmosphereFalloffRayleighId, atmosphereSettings.AtmosphereDensityFalloffRayleigh);
            atmosphereSettings.Material.SetFloat(atmosphereFalloffMieId, atmosphereSettings.AtmosphereDensityFalloffMie);
            atmosphereSettings.Material.SetVector(atmosphereWavelengthsRayleighId, atmosphereSettings.AtmosphereWavelengthsRayleigh);
            atmosphereSettings.Material.SetVector(atmosphereWavelengthsMieId, atmosphereSettings.AtmosphereWavelengthsMie);
            atmosphereSettings.Material.SetFloat(atmosphereSunIntensityId, atmosphereSettings.AtmosphereSunIntensity);

            buffer.DrawMesh(FullscreenMesh, Matrix4x4.identity, atmosphereSettings.Material);
        }

        static void DoBlitPass(CommandBuffer buffer, Material material)
        {
            buffer.DrawMesh(FullscreenMesh, Matrix4x4.identity, material);
        }

        public static void Render(CommandBuffer buffer, RenderTargetIdentifier colorSource, RenderTargetIdentifier depthSource, RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget, PostProcessingSettings postProcessingSettings)
        {
            // TODO: manage 1 or 3 depth textures.
            buffer.SetGlobalTexture(cameraColorTextureId, colorSource);
            buffer.SetGlobalTexture(cameraDepthTextureId, depthSource);
            buffer.SetRenderTarget(
                colorTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );

            if (postProcessingSettings)
            {
                DoAtmospherePass(buffer, postProcessingSettings.AtmosphereSettings);
            }
            else
            {
                DoBlitPass(buffer, CopyMaterial);
            }
        }

        public static void Blit(CommandBuffer buffer, RenderTargetIdentifier colorSource, RenderTargetIdentifier depthSource, RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget)
        {
            buffer.SetGlobalTexture(cameraColorTextureId, colorSource);
            buffer.SetGlobalTexture(cameraDepthTextureId, depthSource);
            buffer.SetRenderTarget(
                colorTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            DoBlitPass(buffer, CopyMaterial);
        }

        static Vector4 GetZBufferParams(Camera camera)
        {
            float x = (camera.farClipPlane - camera.nearClipPlane) / camera.nearClipPlane;
            float y = 1.0f;
            float z = x / camera.farClipPlane;
            float w = 1.0f / camera.farClipPlane;
            return new Vector4(x, y, z, w);

            // float y = camera.farClipPlane / camera.nearClipPlane;
            // float x = 1.0f - y;
            // float z = x / camera.farClipPlane;
            // float w = y / camera.farClipPlane;
            // return new Vector4(x, y, z, w);
        }

        public static void SetMultipleZBufferParams(CommandBuffer buffer, Camera camera1, Camera camera2, Camera camera3)
        {
            buffer.SetGlobalVector(zBufferParams1Id, GetZBufferParams(camera1));
            buffer.SetGlobalVector(zBufferParams2Id, GetZBufferParams(camera2));
            buffer.SetGlobalVector(zBufferParams3Id, GetZBufferParams(camera3));
        }

        public static void RenderDepthCopy(CommandBuffer buffer, Camera camera1, Camera camera2, Camera camera3, RenderTargetIdentifier depthSource1, RenderTargetIdentifier depthSource2, RenderTargetIdentifier depthSource3, RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget)
        {
            buffer.SetRenderTarget(
                colorTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                depthTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
            );
            buffer.SetGlobalVector(zBufferParams1Id, GetZBufferParams(camera1));
            buffer.SetGlobalVector(zBufferParams2Id, GetZBufferParams(camera2));
            buffer.SetGlobalVector(zBufferParams3Id, GetZBufferParams(camera3));
            buffer.SetGlobalTexture(cameraDepthTexture1Id, depthSource1);
            buffer.SetGlobalTexture(cameraDepthTexture2Id, depthSource2);
            buffer.SetGlobalTexture(cameraDepthTexture3Id, depthSource3);
            DoBlitPass(buffer, CopyDepthMaterial);
        }
    }
}
