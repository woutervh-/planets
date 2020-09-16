Shader "Atmosphere Precomputed" {
    Properties {
        [HideInInspector] _PlanetCenter ("Planet center", Vector) = (0, 0, 0)
        [HideInInspector] _PlanetRadius ("Planet radius", Float) = 0.5
        [HideInInspector] _AtmosphereRadius ("Atmosphere radius", Float) = 1
        [HideInInspector] _AtmosphereDensityFalloff ("Atmosphere density falloff", Float) = 1
        [HideInInspector] _AtmosphereWavelengths ("Atmosphere wavelengths", Vector) = (700, 530, 440, 1)
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

            sampler2D _CameraColorTexture;
            sampler2D _CameraDepthTexture;
            sampler2D _OpticalDepthTexture;

            float3 _PlanetCenter;
            float _PlanetRadius;
            float _AtmosphereRadius;
            float _AtmosphereDensityFalloff;
            float4 _AtmosphereWavelengths;
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

            float densityAtPoint(float3 densitySamplePoint) {
                float heightAboveSurface = length(densitySamplePoint - _PlanetCenter) - _PlanetRadius;
                float height01 = heightAboveSurface / (_AtmosphereRadius - _PlanetRadius);
                float localDensity = exp(-height01 * _AtmosphereDensityFalloff) * (1 - height01);
                return localDensity;
            }

            float opticalDepth(float3 rayOrigin, float3 rayDirection) {
                float height = length(_PlanetCenter - rayOrigin) - _PlanetRadius;
                float height01 = height / (_AtmosphereRadius - _PlanetRadius);
                float angle01 = dot(normalize(_PlanetCenter - rayOrigin), rayDirection) * 0.5 + 0.5;
                return tex2Dlod(_OpticalDepthTexture, float4(angle01, height01, 0, 0)).r;
            }

            float3 calculateLight(float3 rayOrigin, float3 rayDirection, float rayLength) {
                float3 sunDirection = TransformWorldToObject(normalize(_MainLightPosition.xyz));
                float cosTheta = dot(sunDirection, rayDirection);
                float3 scatter = pow(400 / _AtmosphereWavelengths.rgb, 4) * _AtmosphereWavelengths.w;

                float3 inScatterPoint = rayOrigin;
                float stepSize = rayLength / (32 - 1);
                float3 inScatteredLight = 0;
                for (int i = 0; i < 32; i++) {
                    float pointToPlanet0;
                    float pointToPlanet1;
                    raySphereIntersect(inScatterPoint, sunDirection, _PlanetCenter, _PlanetRadius, pointToPlanet0, pointToPlanet1);

                    float pointToAtmosphere0;
                    float pointToAtmosphere1;
                    raySphereIntersect(inScatterPoint, sunDirection, _PlanetCenter, _AtmosphereRadius, pointToAtmosphere0, pointToAtmosphere1);

                    pointToAtmosphere0 = max(0, pointToAtmosphere0);
                    pointToAtmosphere1 = max(0, pointToAtmosphere1);
                    float sunRayLength = pointToAtmosphere1 - pointToAtmosphere0;
                    // float sunRayOpticalDepth = opticalDepth(inScatterPoint, sunDirection, sunRayLength);
                    // float viewRayOpticalDepth = opticalDepth(inScatterPoint, -rayDirection, stepSize * i);
                    // TODO: account for hitting the planet.
                    float sunRayOpticalDepth = opticalDepth(inScatterPoint, sunDirection);
                    float viewRayOpticalDepth = opticalDepth(inScatterPoint, -rayDirection);
                    float3 transmittance = pointToPlanet0 > 0 ? 0 : exp(-(sunRayOpticalDepth + viewRayOpticalDepth) * scatter);
                    float localDensity = densityAtPoint(inScatterPoint);

                    inScatteredLight += localDensity * transmittance * scatter * stepSize;
                    inScatterPoint += rayDirection * stepSize;
                }

                return _AtmosphereSunIntensity * inScatteredLight * phaseRayleigh(cosTheta);
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
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.viewVector = mul(unity_CameraInvProjection, float4(input.positionOS.xy * 2 - 1, 0, 1));
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

                // float cameraToPlanet0;
                // float cameraToPlanet1;
                // raySphereIntersect(rayOrigin, rayDirection, _PlanetCenter, _PlanetRadius, cameraToPlanet0, cameraToPlanet1);
                // depth = min(depth, cameraToPlanet0);

                float cameraToAtmosphere0;
                float cameraToAtmosphere1;
                bool atmosphereHit = raySphereIntersect(rayOrigin, rayDirection, _PlanetCenter, _AtmosphereRadius, cameraToAtmosphere0, cameraToAtmosphere1);
                cameraToAtmosphere0 = max(0, cameraToAtmosphere0);
                cameraToAtmosphere1 = max(0, cameraToAtmosphere1);

                float rayLength = min(cameraToAtmosphere1 - cameraToAtmosphere0, depth - cameraToAtmosphere0);

                if (atmosphereHit && rayLength > 0) {
                    const float epsilon = 0.0001;
                    float3 pointInAtmosphere = rayOrigin + rayDirection * (cameraToAtmosphere0 + epsilon);
                    float3 light = calculateLight(pointInAtmosphere, rayDirection, rayLength - epsilon * 2);
                    return float4(color * (1 - light) + light, 1);
                }

                return float4(color, 1);
            }

            ENDHLSL
        }
    }
}
