using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SQT
{
    public class PerlinDisplacementCPU : MeshModifier
    {
        public float strength;
        public float frequency;
        public float lacunarity;
        public float persistence;
        public int octaves;

        Noise.Perlin perlin;
        Texture2D gradientsTexture;
        Texture2D permutationTexture;

        public PerlinDisplacementCPU(int seed)
        {
            perlin = new Noise.Perlin(seed);
            gradientsTexture = Noise.PerlinTextureGenerator.CreateGradientsTexture(perlin);
            permutationTexture = Noise.PerlinTextureGenerator.CreatePermutationTexture(perlin);
        }

        public Task ModifyVertices(SQT.Context context, SQT.Node node, CancellationTokenSource cancellation)
        {
            return Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < node.positions.Length; i++)
                {
                    Noise.Perlin.PerlinSample sample = GetSample(node.positions[i]);
                    node.positions[i] += node.normals[i] * sample.value;
                    node.normals[i] = (node.normals[i] - sample.derivative).normalized;
                }
            }, cancellation.Token);
        }

        public void ModifyMaterial(Material material)
        {
            material.SetTexture("_Gradients2D", gradientsTexture);
            material.SetTexture("_Permutation2D", permutationTexture);
            material.SetFloat("_Strength", strength);
            material.SetFloat("_Frequency", frequency);
            material.SetFloat("_Lacunarity", lacunarity);
            material.SetFloat("_Persistence", persistence);
            material.SetInt("_Octaves", octaves);
        }

        public void Destroy()
        {
            UnityEngine.Object.Destroy(gradientsTexture);
            UnityEngine.Object.Destroy(permutationTexture);
        }

        Noise.Perlin.PerlinSample GetSample(Vector3 position, float frequency)
        {
            Noise.Perlin.PerlinSample sample = perlin.Sample(position * frequency);
            sample.derivative *= frequency;
            return sample;
        }

        Noise.Perlin.PerlinSample GetSample(Vector3 position)
        {
            float strength = this.strength;
            float frequency = this.frequency;

            Noise.Perlin.PerlinSample sum = GetSample(position, frequency) * strength;
            for (int i = 1; i < octaves; i++)
            {
                strength *= persistence;
                frequency *= lacunarity;
                Noise.Perlin.PerlinSample sample = GetSample(position, frequency) * strength;
                sum += sample;
            }
            return sum;
        }
    }
}
