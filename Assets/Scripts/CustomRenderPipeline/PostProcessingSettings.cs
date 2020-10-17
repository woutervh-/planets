using UnityEngine;

namespace CustomRenderPipeline
{
    [CreateAssetMenu(menuName = "Custom Render Pipeline/Post-processing Settings")]
    public class PostProcessingSettings : ScriptableObject
    {
        [SerializeField]
        public PostProcessingEffects.AtmosphereSettings AtmosphereSettings = new PostProcessingEffects.AtmosphereSettings
        {
            PlanetCenter = Vector3.zero,
            PlanetRadius = 0.5f,
            AtmosphereRadius = 1f,
            AtmosphereDensityFalloffRayleigh = 2f,
            AtmosphereDensityFalloffMie = 0.25f,
            AtmosphereWavelengthsRayleigh = new Vector4(700f, 530f, 440f, 20f),
            AtmosphereWavelengthsMie = new Vector4(2800f, 2800f, 2800f, 50f),
            AtmosphereSunIntensity = 10f,
            Precomputed = false,
            Shader = null,
            OpticalDepthTexture = null
        };

#if UNITY_EDITOR
        public void OnValidate()
        {
            if (!AtmosphereSettings.Precomputed)
            {
                AtmosphereSettings.OpticalDepthTexture = null;
                return;
            }
            AtmosphereSettings.OpticalDepthTexture = Atmosphere.AtmosphereTextureGenerator.CreateOpticalDepthTexture(
                AtmosphereSettings.PlanetRadius,
                AtmosphereSettings.AtmosphereRadius,
                AtmosphereSettings.AtmosphereDensityFalloffRayleigh,
                AtmosphereSettings.AtmosphereDensityFalloffMie
            );
            Noise.Perlin perlin = new Noise.Perlin(0);
            AtmosphereSettings.BlueNoiseTexture = Noise.PerlinTextureGenerator.CreateValueTexture(perlin, 2048, 4f);
        }
#endif
    }
}
