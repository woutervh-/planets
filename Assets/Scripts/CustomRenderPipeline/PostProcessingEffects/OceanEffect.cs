using System;
using UnityEngine;

namespace CustomRenderPipeline.PostProcessingEffects
{
    [Serializable]
    public struct OceanSettings
    {
        public Vector3 PlanetCenter;
        public float OceanRadius;
        public float DepthMultiplier;
        public float AlphaMultiplier;
        public Color ShallowColor;
        public Color DeepColor;
        public float Smoothness;
        public float WaveStrengthA;
        public float WaveScaleA;
        public Vector2 WaveVelocityA;
        public Texture WaveNormalMapA;
        public float WaveStrengthB;
        public float WaveScaleB;
        public Vector2 WaveVelocityB;
        public Texture WaveNormalMapB;
        public float WaveStrengthC;
        public float WaveScaleC;
        public Vector2 WaveVelocityC;
        public Texture WaveNormalMapC;
        public float TriplanarMapScale;
        public float TriplanarSharpness;
        public Shader Shader;

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
                    if (material != null)
                    {
                        UnityEngine.Object.DestroyImmediate(material);
                    }
                    material = new Material(Shader) { hideFlags = HideFlags.HideAndDontSave };
                }
                return material;
            }
        }
    }
}
