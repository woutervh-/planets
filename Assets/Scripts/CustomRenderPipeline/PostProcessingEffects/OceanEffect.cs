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
        public Texture WaveNormalMap;
        public float WaveNormalMapScale;
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
