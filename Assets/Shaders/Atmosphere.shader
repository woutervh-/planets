Shader "Atmosphere" {
    Properties {
        _AtmosphereRadius ("Atmosphere radius", Float) = 1
        _PlanetCenter ("Planet center", Vector) = (0, 0, 0)
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
            sampler2D _CameraDepthAttachment;

            // TEXTURE2D_X(_CameraColorTexture);
            // SAMPLER(sampler_CameraColorTexture);

            // TEXTURE2D_X_FLOAT(_CameraDepthAttachment);
            // SAMPLER(sampler_CameraDepthAttachment);

            float3 _PlanetCenter;
            float _AtmosphereRadius;

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

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 rayDirectionOS : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.rayDirectionOS = mul(unity_CameraInvProjection, float4(input.positionOS.xy * 2 - 1, 0, 1)) *_ProjectionParams.z;
                output.rayDirectionOS = mul(unity_CameraToWorld, float4(output.rayDirectionOS, 1));
                return output;
            }

            float4 Frag(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // float3 color = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, UnityStereoTransformScreenSpaceTex(input.uv)).rgb;
                // float sceneDepthNonLinear = SAMPLE_TEXTURE2D_X(_CameraDepthAttachment, sampler_CameraDepthAttachment, UnityStereoTransformScreenSpaceTex(input.uv)).r;
                float3 color = tex2D(_CameraColorTexture, UnityStereoTransformScreenSpaceTex(input.uv)).rgb;
                float sceneDepthNonLinear = tex2D(_CameraDepthAttachment, UnityStereoTransformScreenSpaceTex(input.uv)).r;
                float sceneDepth = LinearEyeDepth(sceneDepthNonLinear, _ZBufferParams);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(input.rayDirectionOS);

                float distanceToAtmosphere0;
                float distanceToAtmosphere1;
                raySphereIntersect(rayOrigin, rayDirection, _PlanetCenter, _AtmosphereRadius, distanceToAtmosphere0, distanceToAtmosphere1);
                float distanceThroughAtmosphere = distanceToAtmosphere1 - distanceToAtmosphere0;
                distanceThroughAtmosphere = min(distanceThroughAtmosphere, sceneDepth - distanceToAtmosphere0);
                float value = distanceThroughAtmosphere / (_AtmosphereRadius * 2);
                
                return float4(sceneDepthNonLinear.xxx, 1);
                // return float4(color.xxx, 1);
            }

            ENDHLSL
        }
    }
}
