using UnityEngine;

namespace Noise
{
    public class PerlinTextureGenerator
    {
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
            texture.Apply();
            return texture;
        }
    }
}
