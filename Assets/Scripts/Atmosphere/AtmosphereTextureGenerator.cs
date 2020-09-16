using UnityEngine;

namespace Atmosphere
{
    public class AtmosphereTextureGenerator
    {
        private const int TEXTURE_RESOLUTION = 256;
        private const int NUM_PRECOMPUTE_STEPS = 32;

        private static bool RaySphereIntersect(Vector3 rayOrigin, Vector3 rayDirection, Vector3 sphereCenter, float sphereRadius, out float t0, out float t1)
        {
            Vector3 L = sphereCenter - rayOrigin;
            float tca = Vector3.Dot(L, rayDirection);
            float r2 = sphereRadius * sphereRadius;
            float d2 = Vector3.Dot(L, L) - tca * tca;

            if (d2 > r2)
            {
                t0 = 0f;
                t1 = Mathf.Infinity;
                return false;
            }

            float thc = Mathf.Sqrt(r2 - d2);
            t0 = Mathf.Max(0, tca - thc);
            t1 = Mathf.Max(0, tca + thc);

            return true;
        }

        // private static float DensityAtPoint(Vector3 densitySamplePoint, float planetRadius, float atmosphereRadius, float atmosphereDensityFalloff)
        // {
        //     float heightAboveSurface = densitySamplePoint.magnitude - planetRadius;
        //     float height01 = heightAboveSurface / (atmosphereRadius - planetRadius);
        //     float localDensity = Mathf.Exp(-height01 * atmosphereDensityFalloff) * (1 - height01);
        //     return localDensity;
        // }

        // private static float CalculateOpticalDepth(Vector3 rayOrigin, Vector3 rayDirection, float rayLength, float planetRadius, float atmosphereRadius, float atmosphereDensityFalloff)
        // {
        //     Vector3 densitySamplePoint = rayOrigin;
        //     float stepSize = rayLength / (NUM_PRECOMPUTE_STEPS - 1);
        //     float opticalDepth = 0;

        //     for (int i = 0; i < NUM_PRECOMPUTE_STEPS; i++)
        //     {
        //         float localDensity = DensityAtPoint(densitySamplePoint, planetRadius, atmosphereRadius, atmosphereDensityFalloff);
        //         opticalDepth += localDensity * stepSize;
        //         densitySamplePoint += rayDirection * stepSize;
        //     }

        //     return opticalDepth;
        // }

        private static float DensityAtPoint(float height01, float atmosphereDensityFalloff)
        {
            return Mathf.Exp(-height01 * atmosphereDensityFalloff) * (1 - height01);
        }

        private static float CalculateOpticalDepth(float height01, float angle01, float planetRadius, float atmosphereRadius, float atmosphereDensityFalloff)
        {
            // height: 0 at planet surface, 1 on atmosphere shell.
            // angle: 0 when looking up from planet, 1 when looking down.
            Vector3 rayOrigin = Vector3.up * planetRadius + Vector3.up * atmosphereRadius * height01;
            Quaternion rotation = Quaternion.AngleAxis(180f * angle01, Vector3.right);
            Vector3 rayDirection = rotation * Vector3.up;

            float pointToAtmosphere0;
            float pointToAtmosphere1;
            RaySphereIntersect(rayOrigin, rayDirection, Vector3.zero, planetRadius + atmosphereRadius, out pointToAtmosphere0, out pointToAtmosphere1);
            float rayLength = pointToAtmosphere1 - pointToAtmosphere0;

            Vector3 densitySamplePoint = rayOrigin;
            float stepSize = rayLength / (NUM_PRECOMPUTE_STEPS - 1);
            float opticalDepth = 0f;
            for (int i = 0; i < NUM_PRECOMPUTE_STEPS; i++)
            {
                float localDensity = DensityAtPoint(height01, atmosphereDensityFalloff);
                opticalDepth += localDensity * stepSize;
                densitySamplePoint += rayDirection * stepSize;
            }

            return opticalDepth;
        }

        public static Texture2D CreateOpticalDepthTexture(float planetRadius, float atmosphereRadius, float atmosphereDensityFalloff)
        {
            Color[] colors = new Color[TEXTURE_RESOLUTION * TEXTURE_RESOLUTION];
            for (int y = 0; y < TEXTURE_RESOLUTION; y++)
            {
                float height01 = (float)y / (TEXTURE_RESOLUTION - 1);
                for (int x = 0; x < TEXTURE_RESOLUTION; x++)
                {
                    float angle01 = (float)x / (TEXTURE_RESOLUTION - 1);
                    float opticalDepth = CalculateOpticalDepth(height01, angle01, planetRadius, atmosphereRadius, atmosphereDensityFalloff);
                    colors[x + y * 256] = new Color(opticalDepth, 0f, 0f);
                }
            }
            Texture2D texture = new Texture2D(256, 256, TextureFormat.RFloat, false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}
