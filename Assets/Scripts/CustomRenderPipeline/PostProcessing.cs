using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class PostProcessing
    {
        static int cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int cameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");

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

        static void DoAtmospherePass(CommandBuffer buffer, PostProcessingEffects.AtmosphereSettings atmosphereSettings, int cameraColorId, int cameraDepthId)
        {
            if (atmosphereSettings.Material == null)
            {
                return;
            }

            buffer.SetGlobalTexture(cameraColorTextureId, cameraColorId);
            buffer.SetGlobalTexture(cameraDepthTextureId, cameraDepthId);
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

        public static void Render(CommandBuffer buffer, Camera camera, PostProcessingSettings postProcessingSettings, int cameraColorId, int cameraDepthId)
        {
            DoAtmospherePass(buffer, postProcessingSettings.AtmosphereSettings, cameraColorId, cameraDepthId);
        }
    }
}
