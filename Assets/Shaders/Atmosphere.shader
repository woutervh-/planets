Shader "Atmosphere" {
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
        [HideInInspector] [Toggle(_PRECOMPUTED_OPTICAL_DEPTH)] _PrecomputedOpticalDepth ("Precomputed optical depth", Float) = 0
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass {
            Name "Atmosphere"

            HLSLPROGRAM

            #pragma shader_feature _PRECOMPUTED_OPTICAL_DEPTH
            #pragma shader_feature _MULTIPLE_DEPTH_TEXTURES

            #define _MULTIPLE_DEPTH_TEXTURES

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./CustomRenderPipeline/Depth.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #define VIEW_RAY_SAMPLES 64

            sampler2D _CameraColorTexture;
            #if defined(_PRECOMPUTED_OPTICAL_DEPTH)
                sampler2D _OpticalDepthTexture;
            #endif

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

            #if defined(_PRECOMPUTED_OPTICAL_DEPTH)
                void opticalDepth(float3 rayOrigin, float3 rayDirection, out float sunRayOpticalDepthRayleigh, out float sunRayOpticalDepthMie) {
                    float height = distance(rayOrigin, _PlanetCenter) - _PlanetRadius;
                    float height01 = height / (_AtmosphereRadius - _PlanetRadius);
                    float angle01 = dot(normalize(_PlanetCenter - rayOrigin), rayDirection) * 0.5 + 0.5;
                    float2 opticalDepth = tex2Dlod(_OpticalDepthTexture, float4(angle01, height01, 0, 0)).rg;
                    sunRayOpticalDepthRayleigh = opticalDepth.r;
                    sunRayOpticalDepthMie = opticalDepth.g;
                }
            #else
                bool opticalDepth(float3 rayOrigin, float3 rayDirection, float rayLength, out float opticalDepthRayleigh, out float opticalDepthMie) {
                    float3 densitySamplePoint = rayOrigin;
                    float stepSize = rayLength / (VIEW_RAY_SAMPLES - 1);
                    opticalDepthRayleigh = 0;
                    opticalDepthMie = 0;

                    for (int i = 0; i < VIEW_RAY_SAMPLES; i++) {
                        float height = distance(densitySamplePoint, _PlanetCenter);
                        if (height < _PlanetRadius) {
                            return false;
                        }
                        opticalDepthRayleigh += densityAtHeightRayleigh(height) * stepSize;
                        opticalDepthMie += densityAtHeightMie(height) * stepSize;
                        densitySamplePoint += rayDirection * stepSize;
                    }

                    return true;
                }
            #endif

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

                    #if defined(_PRECOMPUTED_OPTICAL_DEPTH)
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
                    #else
                        float pointToAtmosphere0;
                        float pointToAtmosphere1;
                        raySphereIntersect(inScatterPoint, sunDirection, _PlanetCenter, _AtmosphereRadius, pointToAtmosphere0, pointToAtmosphere1);
                        pointToAtmosphere0 = max(0, pointToAtmosphere0);
                        pointToAtmosphere1 = max(0, pointToAtmosphere1);

                        float sunRayLength = pointToAtmosphere1 - pointToAtmosphere0;
                        float sunRayOpticalDepthRayleigh;
                        float sunRayOpticalDepthMie;
                        bool overground = opticalDepth(inScatterPoint, sunDirection, sunRayLength, sunRayOpticalDepthRayleigh, sunRayOpticalDepthMie);

                        if (overground) {
                            float3 tau = scatterRayleigh * (sunRayOpticalDepthRayleigh + viewRayOpticalDepthRayleigh) + scatterMie * (sunRayOpticalDepthMie + viewRayOpticalDepthMie);
                            float3 transmittance = exp(-tau);
                            inScatteredLightRayleigh += transmittance * localDensityRayleigh * stepSize;
                            inScatteredLightMie += transmittance * localDensityMie * stepSize;
                        }
                    #endif

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
                if (_ProjectionParams.x < 0) {
                    output.positionCS.y = -output.positionCS.y;
                }
                output.viewVector = mul(unity_CameraInvProjection, float4(input.positionOS.xyz, -1)).xyz;
                output.viewVector = mul(unity_CameraToWorld, float4(output.viewVector, 0)).xyz;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // float depth1 = tex2D(_CameraDepthTexture1, input.uv).r;
                // float depth2 = tex2D(_CameraDepthTexture2, input.uv).r;
                // float depth3 = tex2D(_CameraDepthTexture3, input.uv).r;
                // // depth1 = Linear01DepthFromNear(depth1, _ZBufferParams1);
                // // depth2 = Linear01DepthFromNear(depth2, _ZBufferParams2);
                // // depth3 = Linear01DepthFromNear(depth3, _ZBufferParams3);
                // // return float4(depth1 / _ZBufferParams1.w, depth2 / _ZBufferParams2.w, depth3 / _ZBufferParams3.w, 1);
                // float depthR;
                // if (depth1 > 0) {
                //     return float4(LinearEyeDepth(depth1, _ZBufferParams1), 0, 0, 1);
                // } else {
                //     if (depth2 > 0) {
                //         return float4(0, LinearEyeDepth(depth2, _ZBufferParams2), 0, 1);
                //     } else {
                //         return float4(0, 0, LinearEyeDepth(depth3, _ZBufferParams3), 1);
                //     }
                // }
                // depthR *= length(input.viewVector);
                // return float4(depthR / 200, depthR / 200, depthR / 200, 1);

                float3 color = tex2D(_CameraColorTexture, input.uv).rgb;
                // float depth = tex2D(_CameraDepthTexture, input.uv).r;
                // depth = LinearEyeDepth(depth, _ZBufferParams) * length(input.viewVector);
                float depth = GetLinearEyeDepth(input.uv) * length(input.viewVector);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(input.viewVector);

                float cameraToAtmosphere0;
                float cameraToAtmosphere1;
                bool atmosphereHit = raySphereIntersect(rayOrigin, rayDirection, _PlanetCenter, _AtmosphereRadius, cameraToAtmosphere0, cameraToAtmosphere1);
                cameraToAtmosphere0 = max(0, cameraToAtmosphere0);
                cameraToAtmosphere1 = max(0, cameraToAtmosphere1);

                float rayLength = min(cameraToAtmosphere1 - cameraToAtmosphere0, depth - cameraToAtmosphere0);
                // float rayLength = cameraToAtmosphere1 - cameraToAtmosphere0;

                if (atmosphereHit && rayLength > 0) {
                    float3 pointInAtmosphere = rayOrigin + rayDirection * cameraToAtmosphere0;
                    float3 light = _MainLightColor.rgb * calculateLight(pointInAtmosphere, rayDirection, rayLength);
                    return float4(color * (1 - light) + light, 1);
                }

                return float4(color, 1);
            }

            ENDHLSL
        }
    }
}
