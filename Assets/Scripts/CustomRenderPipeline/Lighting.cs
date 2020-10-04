using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class Lighting
    {
        // Mostly derived from Universal Render Pipeline ForwardLights.cs

        const int maxVisibleAdditionalLights = 256;
        const int maxPerObjectAdditionalLights = 8;
        static Vector4 defaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        static Vector4 defaultLightColor = Color.black;
        static Vector4 defaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        static Vector4 defaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        static Vector4 defaultLightsProbeChannel = new Vector4(-1.0f, 1.0f, -1.0f, -1.0f);

        static int propertyMainLightPosition = Shader.PropertyToID("_MainLightPosition");
        static int propertyMainLightColor = Shader.PropertyToID("_MainLightColor");
        static int propertyAdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
        static int propertyAdditionalLightsPosition = Shader.PropertyToID("_AdditionalLightsPosition");
        static int propertyAdditionalLightsColor = Shader.PropertyToID("_AdditionalLightsColor");
        static int propertyAdditionalLightsAttenuation = Shader.PropertyToID("_AdditionalLightsAttenuation");
        static int propertyAdditionalLightsSpotDir = Shader.PropertyToID("_AdditionalLightsSpotDir");
        static int propertyAdditionalLightOcclusionProbeChannel = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");

        public static void SetupLights(CommandBuffer buffer, NativeArray<VisibleLight> visibleLights, CullingResults cullingResults)
        {
            int mainLightIndex = GetMainLightIndex(visibleLights);
            SetupMainLightConstants(buffer, visibleLights, mainLightIndex);
            SetupAdditionalLightConstants(buffer, visibleLights, mainLightIndex, cullingResults);
        }

        static void SetupMainLightConstants(CommandBuffer buffer, NativeArray<VisibleLight> visibleLights, int mainLightIndex)
        {
            Vector4 lightPosition, lightColor, lightAttenuation, lightSpotDirection, lightOcclusionChannel;
            InitializeLightConstants(visibleLights, mainLightIndex, out lightPosition, out lightColor, out lightAttenuation, out lightSpotDirection, out lightOcclusionChannel);

            buffer.SetGlobalVector(propertyMainLightPosition, lightPosition);
            buffer.SetGlobalVector(propertyMainLightColor, lightColor);
        }

        static void SetupAdditionalLightConstants(CommandBuffer buffer, NativeArray<VisibleLight> visibleLights, int mainLightIndex, CullingResults cullingResults)
        {
            NativeArray<int> indexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
            int globalDirectionalLightsCount = 0;
            int additionalLightsCount = 0;

            for (int i = 0; i < visibleLights.Length; i++)
            {
                if (additionalLightsCount >= maxVisibleAdditionalLights)
                {
                    break;
                }

                VisibleLight light = visibleLights[i];
                if (i == mainLightIndex)
                {
                    indexMap[i] = -1;
                    globalDirectionalLightsCount += 1;
                }
                else
                {
                    indexMap[i] -= globalDirectionalLightsCount;
                    additionalLightsCount += 1;
                }
            }

            for (int i = globalDirectionalLightsCount + additionalLightsCount; i < indexMap.Length; i++)
            {
                indexMap[i] = -1;
            }

            cullingResults.SetLightIndexMap(indexMap);
            indexMap.Dispose();

            Vector4[] additionalLightPositions = new Vector4[maxVisibleAdditionalLights];
            Vector4[] additionalLightColors = new Vector4[maxVisibleAdditionalLights];
            Vector4[] additionalLightAttenuations = new Vector4[maxVisibleAdditionalLights];
            Vector4[] additionalLightSpotDirections = new Vector4[maxVisibleAdditionalLights];
            Vector4[] additionalLightOcclusionProbeChannels = new Vector4[maxVisibleAdditionalLights];
            if (additionalLightsCount > 0)
            {
                for (int i = 0, lightIndex = 0; i < visibleLights.Length && lightIndex < maxVisibleAdditionalLights; i++)
                {
                    VisibleLight light = visibleLights[i];
                    if (mainLightIndex != i)
                    {
                        InitializeLightConstants(visibleLights, i, out additionalLightPositions[lightIndex], out additionalLightColors[lightIndex], out additionalLightAttenuations[lightIndex], out additionalLightSpotDirections[lightIndex], out additionalLightOcclusionProbeChannels[lightIndex]);
                        lightIndex += 1;
                    }
                }

                buffer.SetGlobalVectorArray(propertyAdditionalLightsPosition, additionalLightPositions);
                buffer.SetGlobalVectorArray(propertyAdditionalLightsColor, additionalLightColors);
                buffer.SetGlobalVectorArray(propertyAdditionalLightsAttenuation, additionalLightAttenuations);
                buffer.SetGlobalVectorArray(propertyAdditionalLightsSpotDir, additionalLightSpotDirections);
                buffer.SetGlobalVectorArray(propertyAdditionalLightOcclusionProbeChannel, additionalLightOcclusionProbeChannels);
                buffer.SetGlobalVector(propertyAdditionalLightsCount, new Vector4(maxPerObjectAdditionalLights, 0.0f, 0.0f, 0.0f));
            }
            else
            {
                buffer.SetGlobalVector(propertyAdditionalLightsCount, Vector4.zero);
            }
        }

        static int GetMainLightIndex(NativeArray<VisibleLight> visibleLights)
        {
            int mainLightIndex = -1;
            float mainLightIntensity = 0f;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                if (RenderSettings.sun == visibleLights[i].light)
                {
                    return i;
                }
                if (visibleLights[i].lightType == LightType.Directional && visibleLights[i].light.intensity > mainLightIntensity)
                {
                    mainLightIndex = i;
                    mainLightIntensity = visibleLights[i].light.intensity;
                }
            }
            return mainLightIndex;
        }

        static void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPosition, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDirection, out Vector4 lightOcclusionProbeChannel)
        {
            lightPosition = defaultLightPosition;
            lightColor = defaultLightColor;
            lightAttenuation = defaultLightAttenuation;
            lightSpotDirection = defaultLightSpotDirection;
            lightOcclusionProbeChannel = defaultLightsProbeChannel;

            if (lightIndex < 0)
            {
                return;
            }

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 direction = -lightData.localToWorldMatrix.GetColumn(2);
                lightPosition = new Vector4(direction.x, direction.y, direction.z, 0.0f);
            }
            else
            {
                Vector4 position = lightData.localToWorldMatrix.GetColumn(3);
                lightPosition = new Vector4(position.x, position.y, position.z, 1.0f);
            }

            lightColor = lightData.finalColor;

            if (lightData.lightType != LightType.Directional)
            {
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightData.range * lightData.range);

                lightAttenuation.x = oneOverLightRangeSqr;
                lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 direction = lightData.localToWorldMatrix.GetColumn(2);
                lightSpotDirection = new Vector4(-direction.x, -direction.y, -direction.z, 0.0f);

                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * lightData.spotAngle * 0.5f);
                float cosInnerAngle;
                if (lightData.light != null)
                {
                    cosInnerAngle = Mathf.Cos(lightData.light.innerSpotAngle * Mathf.Deg2Rad * 0.5f);
                }
                else
                {
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(lightData.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                }
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightAttenuation.z = invAngleRange;
                lightAttenuation.w = add;
            }

            Light light = lightData.light;

            int occlusionProbeChannel = light != null ? light.bakingOutput.occlusionMaskChannel : -1;
            lightOcclusionProbeChannel.x = occlusionProbeChannel == -1 ? 0f : occlusionProbeChannel;
            lightOcclusionProbeChannel.y = occlusionProbeChannel == -1 ? 1f : 0f;
        }
    }
}
