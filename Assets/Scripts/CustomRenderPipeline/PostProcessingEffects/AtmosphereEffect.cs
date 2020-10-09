using System;
using UnityEngine;

namespace CustomRenderPipeline.PostProcessingEffects
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
}
