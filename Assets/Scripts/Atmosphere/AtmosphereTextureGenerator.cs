using UnityEngine;

namespace Atmosphere
{
    public class AtmosphereTextureGenerator
    {
        // float densityAtPoint(float3 densitySamplePoint) {
        //     float heightAboveSurface = length(densitySamplePoint - _PlanetCenter) - _PlanetRadius;
        //     float height01 = heightAboveSurface / (_AtmosphereRadius - _PlanetRadius);
        //     float localDensity = exp(-height01 * _AtmosphereDensityFalloff) * (1 - height01);
        //     return localDensity;
        // }

        // float opticalDepth(float3 rayOrigin, float3 rayDirection, float rayLength) {
        //     float3 densitySamplePoint = rayOrigin;
        //     float stepSize = rayLength / (10 - 1);
        //     float opticalDepth = 0;

        //     for (int i = 0; i < 10; i++) {
        //         float localDensity = densityAtPoint(densitySamplePoint);
        //         opticalDepth += localDensity * stepSize;
        //         densitySamplePoint += rayDirection * stepSize;
        //     }

        //     return opticalDepth;
        // }

        private static float CalculateOpticalDepth(float height01, float angle01, float planetRadius, float atmosphereRadius)
        {
            // height: 0 at planet surface, 1 on atmosphere shell.
            // angle: 0 when looking up from planet, 1 when looking down.
            Vector3 rayOrigin = Vector3.up * planetRadius + Vector3.up * atmosphereRadius * height01;
            Quaternion.EulerRotation()
            Vector3 rayDirection = Vector3.RotateTowards();
        }

        public static Texture2D CreateTexture()
        {
            Color[] colors = new Color[256 * 256];
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    colors[x + y * 256] = ;
                }
            }
            Texture2D texture = new Texture2D(256, 256, TextureFormat.R8, false, true);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }
    }
}
