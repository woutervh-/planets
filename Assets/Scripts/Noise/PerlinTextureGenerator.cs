using UnityEngine;

namespace Noise
{
    public class PerlinTextureGenerator
    {
        public static Texture2D CreateValueTexture(Perlin perlin, int resolution, float frequency)
        {
            Color[] colors = new Color[resolution * resolution];
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    if (perlin.Value(new Vector3(x, y, 0f)) > 0f)
                    {
                        Debug.Log(perlin.Value(new Vector3(x, y, 0f)));
                    }
                    Vector3 position = frequency * new Vector3((float)x / resolution * Perlin.SIZE, (float)y / resolution * Perlin.SIZE, 0f);
                    colors[x + y * resolution] = Color.white * (0.5f + 0.5f * perlin.Value(position));
                }
            }
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.R16, false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.SetPixels(colors);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateGradientsTexture(Perlin perlin)
        {
            Vector3[] gradients = perlin.GetGradients();
            Color[] colors = new Color[Perlin.SIZE];
            for (int x = 0; x < Perlin.SIZE; x++)
            {
                Vector3 gradient = (gradients[x] + Vector3.one) / 2f;
                colors[x] = new Color(gradient.x, gradient.y, gradient.z, 1f);
            }
            Texture2D texture = new Texture2D(Perlin.SIZE, 1, TextureFormat.RGBA32, false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.SetPixels(colors);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.Apply();
            return texture;
        }

        public static Texture2D CreatePermutationTexture(Perlin perlin)
        {
            Vector4Int[] hashes = perlin.GetHashes2D();
            Color[] colors = new Color[Perlin.SIZE * Perlin.SIZE];
            for (int y = 0; y < Perlin.SIZE; y++)
            {
                for (int x = 0; x < Perlin.SIZE; x++)
                {
                    colors[x + y * Perlin.SIZE] = hashes[x + y * Perlin.SIZE] / 255f;
                }
            }
            Texture2D texture = new Texture2D(Perlin.SIZE, Perlin.SIZE, TextureFormat.RGBA32, false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.SetPixels(colors);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.Apply();
            return texture;
        }
    }
}
