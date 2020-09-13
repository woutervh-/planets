using UnityEngine;

namespace Noise
{
    public class Perlin
    {
        public class PerlinSample
        {
            public float value;
            public Vector3 derivative;

            public static PerlinSample operator +(PerlinSample lhs, PerlinSample rhs)
            {
                lhs.value += rhs.value;
                lhs.derivative += rhs.derivative;
                return lhs;
            }

            public static PerlinSample operator +(PerlinSample lhs, float rhs)
            {
                lhs.value += rhs;
                return lhs;
            }

            public static PerlinSample operator +(float lhs, PerlinSample rhs)
            {
                rhs.value += lhs;
                return rhs;
            }

            public static PerlinSample operator -(PerlinSample lhs, PerlinSample rhs)
            {
                lhs.value -= rhs.value;
                lhs.derivative -= rhs.derivative;
                return lhs;
            }

            public static PerlinSample operator -(PerlinSample lhs, float rhs)
            {
                lhs.value -= rhs;
                return lhs;
            }

            public static PerlinSample operator -(float lhs, PerlinSample rhs)
            {
                rhs.value = lhs - rhs.value;
                rhs.derivative = -rhs.derivative;
                return rhs;
            }

            public static PerlinSample operator *(PerlinSample lhs, PerlinSample rhs)
            {
                lhs.derivative = lhs.derivative * rhs.value + rhs.derivative * lhs.value;
                lhs.value *= rhs.value;
                return lhs;
            }

            public static PerlinSample operator *(PerlinSample lhs, float rhs)
            {
                lhs.value *= rhs;
                lhs.derivative *= rhs;
                return lhs;
            }

            public static PerlinSample operator *(float lhs, PerlinSample rhs)
            {
                rhs.value *= lhs;
                rhs.derivative *= lhs;
                return rhs;
            }

            public static PerlinSample operator /(PerlinSample lhs, PerlinSample rhs)
            {
                lhs.derivative = (lhs.derivative * rhs.value - rhs.derivative * lhs.value) / (rhs.value * rhs.value);
                lhs.value /= rhs.value;
                return lhs;
            }

            public static PerlinSample operator /(PerlinSample lhs, float rhs)
            {
                lhs.value /= rhs;
                lhs.derivative /= rhs;
                return lhs;
            }

            public static PerlinSample operator /(float lhs, PerlinSample rhs)
            {
                rhs.value /= lhs;
                rhs.derivative /= lhs;
                return rhs;
            }
        }

        public const int SIZE = 256;
        private int[] permutation;
        private Vector4Int[] hashes;
        private Vector3[] gradients;

        public Perlin(int seed)
        {
            Random.InitState(seed);

            int[] permutation = new int[Perlin.SIZE];
            for (int i = 0; i < Perlin.SIZE; i++)
            {
                permutation[i] = i;
            }
            ArrayUtilities.Shuffle(permutation, seed);

            this.permutation = new int[Perlin.SIZE * 2];
            for (int i = 0; i < Perlin.SIZE; i++)
            {
                this.permutation[i] = permutation[i];
                this.permutation[i + Perlin.SIZE] = permutation[i];
            }

            this.hashes = new Vector4Int[Perlin.SIZE * Perlin.SIZE];
            for (int ix = 0; ix < Perlin.SIZE; ix++)
            {
                for (int iy = 0; iy < Perlin.SIZE; iy++)
                {
                    int a = this.permutation[ix] + iy;
                    int aa = this.permutation[a];
                    int ab = this.permutation[a + 1];
                    int b = this.permutation[ix + 1] + iy;
                    int ba = this.permutation[b];
                    int bb = this.permutation[b + 1];
                    this.hashes[ix + iy * Perlin.SIZE] = new Vector4Int(aa, ab, ba, bb);
                }
            }

            this.gradients = new Vector3[Perlin.SIZE * 2];
            for (int i = 0; i < Perlin.SIZE; i++)
            {
                this.gradients[i + Perlin.SIZE] = this.gradients[i] = Perlin.gradientLookup[this.permutation[i] % 12];
            }
        }

        public int[] GetPermutation()
        {
            return this.permutation;
        }

        public Vector4Int[] GetHashes2D()
        {
            return this.hashes;
        }

        public Vector3[] GetGradients()
        {
            return this.gradients;
        }

        public float Value(Vector3 position)
        {
            int ix = Perlin.FlooredRemainder(Mathf.FloorToInt(position.x), Perlin.SIZE);
            int iy = Perlin.FlooredRemainder(Mathf.FloorToInt(position.y), Perlin.SIZE);
            int iz = Perlin.FlooredRemainder(Mathf.FloorToInt(position.z), Perlin.SIZE);
            float fx = position.x - Mathf.FloorToInt(position.x);
            float fy = position.y - Mathf.FloorToInt(position.y);
            float fz = position.z - Mathf.FloorToInt(position.z);
            float u = Perlin.Fade(fx);
            float v = Perlin.Fade(fy);
            float w = Perlin.Fade(fz);

            int aa = this.hashes[ix + iy * Perlin.SIZE].x + iz;
            int ab = this.hashes[ix + iy * Perlin.SIZE].y + iz;
            int ba = this.hashes[ix + iy * Perlin.SIZE].z + iz;
            int bb = this.hashes[ix + iy * Perlin.SIZE].w + iz;

            Vector3 g000 = this.gradients[aa];
            Vector3 g100 = this.gradients[ba];
            Vector3 g010 = this.gradients[ab];
            Vector3 g110 = this.gradients[bb];
            Vector3 g001 = this.gradients[aa + 1];
            Vector3 g101 = this.gradients[ba + 1];
            Vector3 g011 = this.gradients[ab + 1];
            Vector3 g111 = this.gradients[bb + 1];

            Vector3 p = new Vector3(fx, fy, fz);
            float v000 = Vector3.Dot(g000, p + directionLookup[0]);
            float v100 = Vector3.Dot(g100, p + directionLookup[1]);
            float v010 = Vector3.Dot(g010, p + directionLookup[2]);
            float v110 = Vector3.Dot(g110, p + directionLookup[3]);
            float v001 = Vector3.Dot(g001, p + directionLookup[4]);
            float v101 = Vector3.Dot(g101, p + directionLookup[5]);
            float v011 = Vector3.Dot(g011, p + directionLookup[6]);
            float v111 = Vector3.Dot(g111, p + directionLookup[7]);

            float a = v000;
            float b = v100 - v000;
            float c = v010 - v000;
            float d = v001 - v000;
            float e = v110 - v010 - v100 + v000;
            float f = v101 - v001 - v100 + v000;
            float g = v011 - v001 - v010 + v000;
            float h = v111 - v011 - v101 + v001 - v110 + v010 + v100 - v000;

            return a + b * u + (c + e * u) * v + (d + f * u + (g + h * u) * v) * w;
        }

        public PerlinSample Sample(Vector3 position)
        {
            int ix = Perlin.FlooredRemainder(Mathf.FloorToInt(position.x), Perlin.SIZE);
            int iy = Perlin.FlooredRemainder(Mathf.FloorToInt(position.y), Perlin.SIZE);
            int iz = Perlin.FlooredRemainder(Mathf.FloorToInt(position.z), Perlin.SIZE);
            float fx = position.x - Mathf.FloorToInt(position.x);
            float fy = position.y - Mathf.FloorToInt(position.y);
            float fz = position.z - Mathf.FloorToInt(position.z);
            float u = Perlin.Fade(fx);
            float v = Perlin.Fade(fy);
            float w = Perlin.Fade(fz);
            float du = Perlin.FadeDerivative(fx);
            float dv = Perlin.FadeDerivative(fy);
            float dw = Perlin.FadeDerivative(fz);

            int aa = this.hashes[ix + iy * Perlin.SIZE].x + iz;
            int ab = this.hashes[ix + iy * Perlin.SIZE].y + iz;
            int ba = this.hashes[ix + iy * Perlin.SIZE].z + iz;
            int bb = this.hashes[ix + iy * Perlin.SIZE].w + iz;

            Vector3 g000 = this.gradients[aa];
            Vector3 g100 = this.gradients[ba];
            Vector3 g010 = this.gradients[ab];
            Vector3 g110 = this.gradients[bb];
            Vector3 g001 = this.gradients[aa + 1];
            Vector3 g101 = this.gradients[ba + 1];
            Vector3 g011 = this.gradients[ab + 1];
            Vector3 g111 = this.gradients[bb + 1];

            Vector3 p = new Vector3(fx, fy, fz);
            float v000 = Vector3.Dot(g000, p + directionLookup[0]);
            float v100 = Vector3.Dot(g100, p + directionLookup[1]);
            float v010 = Vector3.Dot(g010, p + directionLookup[2]);
            float v110 = Vector3.Dot(g110, p + directionLookup[3]);
            float v001 = Vector3.Dot(g001, p + directionLookup[4]);
            float v101 = Vector3.Dot(g101, p + directionLookup[5]);
            float v011 = Vector3.Dot(g011, p + directionLookup[6]);
            float v111 = Vector3.Dot(g111, p + directionLookup[7]);

            float a = v000;
            float b = v100 - v000;
            float c = v010 - v000;
            float d = v001 - v000;
            float e = v110 - v010 - v100 + v000;
            float f = v101 - v001 - v100 + v000;
            float g = v011 - v001 - v010 + v000;
            float h = v111 - v011 - v101 + v001 - v110 + v010 + v100 - v000;

            Vector3 da = g000;
            Vector3 db = g100 - g000;
            Vector3 dc = g010 - g000;
            Vector3 dd = g001 - g000;
            Vector3 de = g110 - g010 - g100 + g000;
            Vector3 df = g101 - g001 - g100 + g000;
            Vector3 dg = g011 - g001 - g010 + g000;
            Vector3 dh = g111 - g011 - g101 + g001 - g110 + g010 + g100 - g000;

            PerlinSample sample = new PerlinSample();
            sample.value = a + b * u + (c + e * u) * v + (d + f * u + (g + h * u) * v) * w;
            sample.derivative = da + db * u + (dc + de * u) * v + (dd + df * u + (dg + dh * u) * v) * w;
            sample.derivative.x += du * (b + e * v + (f + h * v) * w);
            sample.derivative.y += dv * (c + e * u + (g + h * u) * w);
            sample.derivative.z += dw * (d + f * u + (g + h * u) * v);

            return sample;
        }

        private static int FlooredRemainder(int a, int n)
        {
            if (a >= 0)
            {
                return a % n;
            }
            else
            {
                return (n + a % n) % n;
            }
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float FadeDerivative(float t)
        {
            return 30.0f * t * t * (t * (t - 2.0f) + 1.0f);
        }

        private static Vector3[] gradientLookup = new Vector3[] {
            new Vector3(1, 1, 0).normalized,
            new Vector3(-1, 1, 0).normalized,
            new Vector3(1, -1, 0).normalized,
            new Vector3(-1, -1, 0).normalized,
            new Vector3(1, 0, 1).normalized,
            new Vector3(-1, 0, 1).normalized,
            new Vector3(1, 0, -1).normalized,
            new Vector3(-1, 0, -1).normalized,
            new Vector3(0, 1, 1).normalized,
            new Vector3(0, -1, 1).normalized,
            new Vector3(0, 1, -1).normalized,
            new Vector3(0, -1, -1).normalized
        };

        private static Vector3[] directionLookup = new Vector3[] {
            new Vector3(0, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, -1, 0),
            new Vector3(-1, -1, 0),
            new Vector3(0, 0, -1),
            new Vector3(-1, 0, -1),
            new Vector3(0, -1, -1),
            new Vector3(-1, -1, -1)
        };
    }
}
