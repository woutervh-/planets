using System;
using UnityEngine;

namespace CustomRenderPipeline
{
    [CreateAssetMenu(menuName = "Custom Render Pipeline/Post-processing Settings")]
    public class PostProcessingSettings : ScriptableObject
    {
        [Serializable]
        public struct AtmosphereSettings
        {
            public Vector3 PlanetCenter;
            public float PlanetRadius;
            public float AtmosphereRadius;
            public float AtmosphereDensityFalloffRayleigh;
            public float AtmosphereDensityFalloffMie;
            public Vector4 AtmosphereWavelengthsRayleigh;
            public Vector4 AtmosphereWavelengthsMie;
            public float AtmosphereSunIntensity;
            public bool Precomputed;
            public Shader Shader;

            private Texture2D opticalDepthTexture;
            public Texture2D OpticalDepthTexture
            {
                get
                {
                    return opticalDepthTexture;
                }

                set
                {
                    opticalDepthTexture = value;
                }
            }

            private Material material;
            public Material Material
            {
                get
                {
                    if (Shader == null)
                    {
                        return null;
                    }
                    if (material == null || material.shader != Shader)
                    {
                        material = new Material(Shader) { hideFlags = HideFlags.HideAndDontSave };
                    }
                    return material;
                }
            }
        }

        [SerializeField]
        public AtmosphereSettings Atmosphere = new AtmosphereSettings
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
            Atmosphere. CreateOpticalDepthTexture
        }
#endif
    }
}
