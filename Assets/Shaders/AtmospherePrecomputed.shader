Shader "Atmosphere/Precomputed" {
    Properties {
        [HideInInspector] _PlanetCenter ("Planet center", Vector) = (0, 0, 0)
        [HideInInspector] _PlanetRadius ("Planet radius", Float) = 0.5
        [HideInInspector] _AtmosphereRadius ("Atmosphere radius", Float) = 1
        [HideInInspector] _AtmosphereFalloffRayleigh ("Atmosphere falloff Rayleigh", Float) = 2
        [HideInInspector] _AtmosphereFalloffMie ("Atmosphere falloff Mie", Float) = 0.25
        [HideInInspector] _AtmosphereWavelengthsRayleigh ("Atmosphere wavelengths", Vector) = (700, 530, 440, 20)
        [HideInInspector] _AtmosphereWavelengthsMie ("Atmosphere wavelengths", Vector) = (2800, 2800, 2800, 50)
        [HideInInspector] _AtmosphereSunIntensity ("Atmosphere sun intensity", Float) = 10
        [HideInInspector] _OpticalDepthTexture ("Optical depth texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass {
            Name "Atmosphere"

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #define VIEW_RAY_SAMPLES 64

            sampler2D _CameraColorTexture;
            sampler2D _CameraDepthTexture;
            sampler2D _OpticalDepthTexture;

            float3 _PlanetCenter;
            float _PlanetRadius;
            float _AtmosphereRadius;
            float _AtmosphereFalloffRayleigh;
            float _AtmosphereFalloffMie;
            float4 _AtmosphereWavelengthsRayleigh;
            float4 _AtmosphereWavelengthsMie;
            float _AtmosphereSunIntensity;

            bool raySphereIntersect(float3 rayOrigin, float3 rayDirection, float3 sphereCenter, float sphereRadius, out float t0, out float t1) {
                float3 L = sphereCenter - rayOrigin;
                float tca = dot(L, rayDirection);
                float r2 = sphereRadius * sphereRadius;
                float d2 = dot(L, L) - tca * tca;
                
                if (d2 > r2) {
                    return false;
                }
                
                float thc = sqrt(r2 - d2);
                t0 = tca - thc;
                t1 = tca + thc;
                
                return true;
            }

            float phaseRayleigh(float cosTheta) {
                return 3.0 / (16.0 * PI) * (1.0 + cosTheta * cosTheta);
            }

            float phaseMie(float cosTheta) {
                const float g = 0.75;
                return (1.0 - g * g) / ((4.0 * PI) * pow(1.0 + g * g - 2.0 * g * cosTheta, 1.5));
            }

            float densityAtHeightRayleigh(float height) {
                return exp(-height * _AtmosphereFalloffRayleigh / _AtmosphereRadius);
            }

            float densityAtHeightMie(float height) {
                return exp(-height * _AtmosphereFalloffMie / _AtmosphereRadius);
            }

            void opticalDepth(float3 rayOrigin, float3 rayDirection, out float sunRayOpticalDepthRayleigh, out float sunRayOpticalDepthMie) {
                float height = distance(rayOrigin, _PlanetCenter) - _PlanetRadius;
                float height01 = height / (_AtmosphereRadius - _PlanetRadius);
                float angle01 = dot(normalize(_PlanetCenter - rayOrigin), rayDirection) * 0.5 + 0.5;
                float2 opticalDepth = tex2Dlod(_OpticalDepthTexture, float4(angle01, height01, 0, 0)).rg;
                sunRayOpticalDepthRayleigh = opticalDepth.r;
                sunRayOpticalDepthMie = opticalDepth.g;
            }

            float3 calculateLight(float3 rayOrigin, float3 rayDirection, float rayLength) {
                float3 sunDirection = TransformWorldToObject(normalize(_MainLightPosition.xyz));
                float cosTheta = dot(sunDirection, rayDirection);
                float3 scatterRayleigh = pow(400 / _AtmosphereWavelengthsRayleigh.rgb, 4) * _AtmosphereWavelengthsRayleigh.w;
                float3 scatterMie = pow(400 / _AtmosphereWavelengthsMie.rgb, 4) * _AtmosphereWavelengthsMie.w;

                float3 inScatterPoint = rayOrigin;
                float stepSize = rayLength / (VIEW_RAY_SAMPLES - 1);
                float3 inScatteredLightRayleigh = 0;
                float3 inScatteredLightMie = 0;
                float viewRayOpticalDepthRayleigh = 0;
                float viewRayOpticalDepthMie = 0;
                for (int i = 0; i < VIEW_RAY_SAMPLES; i++) {
                    float height = distance(inScatterPoint, _PlanetCenter);
                    float localDensityRayleigh = densityAtHeightRayleigh(height);
                    float localDensityMie = densityAtHeightMie(height);
                    viewRayOpticalDepthRayleigh += localDensityRayleigh * stepSize;
                    viewRayOpticalDepthMie += localDensityMie * stepSize;

                    float pointToPlanet0 = 0;
                    float pointToPlanet1 = 0;
                    bool planetHit = raySphereIntersect(inScatterPoint, sunDirection, _PlanetCenter, _PlanetRadius, pointToPlanet0, pointToPlanet1);

                    if (!(pointToPlanet0 > 0.0 || pointToPlanet1 > 0.0)) {
                        float sunRayOpticalDepthRayleigh;
                        float sunRayOpticalDepthMie;
                        opticalDepth(inScatterPoint, sunDirection, sunRayOpticalDepthRayleigh, sunRayOpticalDepthMie);
                        float3 tau = scatterRayleigh * (sunRayOpticalDepthRayleigh + viewRayOpticalDepthRayleigh) + scatterMie * (sunRayOpticalDepthMie + viewRayOpticalDepthMie);
                        float3 transmittance = exp(-tau);
                        inScatteredLightRayleigh += transmittance * localDensityRayleigh * stepSize;
                        inScatteredLightMie += transmittance * localDensityMie * stepSize;
                    }

                    inScatterPoint += rayDirection * stepSize;
                }

                return _AtmosphereSunIntensity * (scatterRayleigh * inScatteredLightRayleigh * phaseRayleigh(cosTheta) + scatterMie * inScatteredLightMie * phaseMie(cosTheta));
            }

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVector : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = input.positionOS;
                output.uv = input.uv;
                output.viewVector = mul(unity_CameraInvProjection, float4(input.positionOS.xyz, -1));
                output.viewVector = mul(unity_CameraToWorld, float4(output.viewVector, 0));
                return output;
            }

            float4 Frag(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 color = tex2D(_CameraColorTexture, input.uv).rgb;
                float depth = tex2D(_CameraDepthTexture, input.uv).r;
                depth = LinearEyeDepth(depth, _ZBufferParams) * length(input.viewVector);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(input.viewVector);

                float cameraToAtmosphere0;
                float cameraToAtmosphere1;
                bool atmosphereHit = raySphereIntersect(rayOrigin, rayDirection, _PlanetCenter, _AtmosphereRadius, cameraToAtmosphere0, cameraToAtmosphere1);
                cameraToAtmosphere0 = max(0, cameraToAtmosphere0);
                cameraToAtmosphere1 = max(0, cameraToAtmosphere1);

                float rayLength = min(cameraToAtmosphere1 - cameraToAtmosphere0, depth - cameraToAtmosphere0);

                if (atmosphereHit && rayLength > 0) {
                    float3 pointInAtmosphere = rayOrigin + rayDirection * cameraToAtmosphere0;
                    float3 light = _MainLightColor * calculateLight(pointInAtmosphere, rayDirection, rayLength);
                    return float4(color * (1 - light) + light, 1);
                }

                return float4(color, 1);
            }

            ENDHLSL
        }
    }
}
