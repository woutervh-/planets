using UnityEngine;
using System;

namespace SQT.Displacement
{
    [Serializable]
    public class Settings
    {
        public DisplacementType displacementType;
        public ComputeShader computeShader;
        public int seed = 0;
        public float strength = 0.1f;
        public float frequency = 1f;
        public float lacunarity = 2f;
        public float persistence = 0.5f;
        public int octaves = 8;

        public MeshModifier GetMeshModifier()
        {
            switch (displacementType)
            {
                case DisplacementType.None:
                    return new NoneDisplacement();
                case DisplacementType.Perlin:
                    {
                        if (computeShader == null)
                        {
                            return null;
                        }
                        MeshModifier meshModifier = new PerlinDisplacementGPU(seed, computeShader)
                        {
                            strength = strength,
                            frequency = frequency,
                            lacunarity = lacunarity,
                            persistence = persistence,
                            octaves = octaves
                        };
                        return meshModifier;
                    }

            }
            return null;
        }

        public enum DisplacementType
        {
            None,
            Perlin
        }
    }
}
