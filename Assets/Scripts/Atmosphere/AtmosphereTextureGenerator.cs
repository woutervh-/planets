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
            t0 = tca - thc;
            t1 = tca + thc;

            return true;
        }

        private static float DensityAtHeightRayleigh(float height, float atmosphereDensityFalloffRayleigh, float atmosphereRadius)
        {
            return Mathf.Exp(-height * atmosphereDensityFalloffRayleigh / atmosphereRadius);
        }

        private static float DensityAtHeightMie(float height, float atmosphereDensityFalloffMie, float atmosphereRadius)
        {
            return Mathf.Exp(-height * atmosphereDensityFalloffMie / atmosphereRadius);
        }

        private static Vector2 CalculateOpticalDepth(float height01, float angle01, float planetRadius, float atmosphereRadius, float atmosphereDensityFalloffRayleigh, float atmosphereDensityFalloffMie)
        {
            // TODO: make texture coordinates logarithmic to add better detail for transitions.
            // height: 0 at planet surface, 1 on atmosphere shell.
            // angle: 0 when looking up from planet, 1 when looking down.
            Vector3 rayOrigin = Vector3.up * (planetRadius + (atmosphereRadius - planetRadius) * height01);
            float theta = Mathf.Acos((angle01 - 0.5f) * 2f);
            Quaternion rotation = Quaternion.AngleAxis(theta / Mathf.PI * 180f, Vector3.right);
            Vector3 rayDirection = rotation * Vector3.down;

            float pointToAtmosphere0;
            float pointToAtmosphere1;
            RaySphereIntersect(rayOrigin, rayDirection, Vector3.zero, atmosphereRadius, out pointToAtmosphere0, out pointToAtmosphere1);
            pointToAtmosphere0 = Mathf.Max(0, pointToAtmosphere0);
            pointToAtmosphere1 = Mathf.Max(0, pointToAtmosphere1);
            float rayLength = pointToAtmosphere1 - pointToAtmosphere0;

            Vector3 densitySamplePoint = rayOrigin;
            float stepSize = rayLength / (NUM_PRECOMPUTE_STEPS - 1);
            float opticalDepthRayleigh = 0f;
            float opticalDepthMie = 0f;
            for (int i = 0; i < NUM_PRECOMPUTE_STEPS; i++)
            {
                float height = densitySamplePoint.magnitude;
                if (height < planetRadius)
                {
                    return Vector2.positiveInfinity;
                }
                float localDensityRayleigh = DensityAtHeightRayleigh(height, atmosphereDensityFalloffRayleigh, atmosphereRadius);
                float localDensityMie = DensityAtHeightMie(height, atmosphereDensityFalloffMie, atmosphereRadius);
                opticalDepthRayleigh += localDensityRayleigh * stepSize;
                opticalDepthMie += localDensityMie * stepSize;
                densitySamplePoint += rayDirection * stepSize;
            }

            return new Vector2(opticalDepthRayleigh, opticalDepthMie);
        }

        public static Texture2D CreateOpticalDepthTexture(float planetRadius, float atmosphereRadius, float atmosphereDensityFalloffRayleigh, float atmosphereDensityFalloffMie)
        {
            Color[] colors = new Color[TEXTURE_RESOLUTION * TEXTURE_RESOLUTION];
            for (int y = 0; y < TEXTURE_RESOLUTION; y++)
            {
                float height01 = (float)y / (TEXTURE_RESOLUTION - 1);
                for (int x = 0; x < TEXTURE_RESOLUTION; x++)
                {
                    float angle01 = (float)x / (TEXTURE_RESOLUTION - 1);
                    Vector2 opticalDepth = CalculateOpticalDepth(height01, angle01, planetRadius, atmosphereRadius, atmosphereDensityFalloffRayleigh, atmosphereDensityFalloffMie);
                    colors[x + y * TEXTURE_RESOLUTION] = new Color(opticalDepth.x, opticalDepth.y, 0f);
                }
            }
            Texture2D texture = new Texture2D(TEXTURE_RESOLUTION, TEXTURE_RESOLUTION, TextureFormat.RGHalf, false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}
