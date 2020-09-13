using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SQT
{
    public class PerlinDisplacementCPU : VerticesModifier
    {
        public int seed = 0;
        public float strength = 0.1f;
        public float frequency = 1f;
        public float lacunarity = 2f;
        public float persistence = 0.5f;
        public int octaves = 8;

        Noise.Perlin perlin;

        public PerlinDisplacementCPU()
        {
            perlin = new Noise.Perlin(seed);
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
