Shader "Ocean" {
    Properties {
        [HideInInspector] _PlanetCenter ("Planet center", Vector) = (0, 0, 0)
        [HideInInspector] _OceanRadius ("Ocean radius", Float) = 0.5
        [HideInInspector] _DepthMultiplier ("Depth multiplier", Float) = 1
        [HideInInspector] _AlphaMultiplier ("Alpha multiplier", Float) = 1
        [HideInInspector] _ShallowColor ("Shallow color", Color) = (1, 1, 1, 1)
        [HideInInspector] _DeepColor ("Deep color", Color) = (0, 0, 0, 1)
        [HideInInspector] _Smoothness ("Smoothness", Float) = 0

        [HideInInspector] _WaveStrengthA ("Wave strength A", Float) = 1
        [HideInInspector] _WaveScaleA ("Wave scale A", Float) = 1
        [HideInInspector] _WaveVelocityA ("Wave velocity A", Vector) = (0, 1, 0, 0)
        [HideInInspector] _WaveNormalMapA ("Wave normal map A", 2D) = "bump" {}
        [HideInInspector] _WaveStrengthB ("Wave strength B", Float) = 1
        [HideInInspector] _WaveScaleB ("Wave scale B", Float) = 1
        [HideInInspector] _WaveVelocityB ("Wave velocity B", Vector) = (1, 0, 0, 0)
        [HideInInspector] _WaveNormalMapB ("Wave normal map B", 2D) = "bump" {}
        [HideInInspector] _WaveStrengthC ("Wave strength C", Float) = 1
        [HideInInspector] _WaveScaleC ("Wave scale C", Float) = 1
        [HideInInspector] _WaveVelocityC ("Wave velocity C", Vector) = (0, -1, 0, 0)
        [HideInInspector] _WaveNormalMapC ("Wave normal map C", 2D) = "bump" {}
        [HideInInspector] _TriplanarMapScale ("Triplanar map scale", Float) = 1
        [HideInInspector] _TriplanarSharpness ("Triplanar sharpness", Float) = 1.0
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass {
            Name "Ocean"

            HLSLPROGRAM

            #pragma shader_feature _MULTIPLE_DEPTH_TEXTURES

            #define _NORMALMAP
            #define _TRIPLANAR_MAPPING

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "./CustomRenderPipeline/Depth.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            #define VIEW_RAY_SAMPLES 64

            sampler2D _CameraColorTexture;

            float3 _PlanetCenter;
            float _OceanRadius;
            float _DepthMultiplier;
            float _AlphaMultiplier;
            float4 _ShallowColor;
            float4 _DeepColor;
            float _TriplanarMapScale;
            float _TriplanarSharpness;
            float _WaveStrengthA;
            float _WaveScaleA;
            float2 _WaveVelocityA;
            TEXTURE2D(_WaveNormalMapA); SAMPLER(sampler_WaveNormalMapA);
            float _WaveStrengthB;
            float _WaveScaleB;
            float2 _WaveVelocityB;
            TEXTURE2D(_WaveNormalMapB); SAMPLER(sampler_WaveNormalMapB);
            float _WaveStrengthC;
            float _WaveScaleC;
            float2 _WaveVelocityC;
            TEXTURE2D(_WaveNormalMapC); SAMPLER(sampler_WaveNormalMapC);

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

                float4 color = tex2D(_CameraColorTexture, input.uv);
                float depth = GetLinearEyeDepth(input.uv) * length(input.viewVector);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(input.viewVector);
                float3 sunDirection = TransformWorldToObject(normalize(_MainLightPosition.xyz));

                float cameraToOcean0;
                float cameraToOcean1;
                raySphereIntersect(rayOrigin, rayDirection, _PlanetCenter, _OceanRadius, cameraToOcean0, cameraToOcean1);
                cameraToOcean0 = max(0, cameraToOcean0);
                cameraToOcean1 = max(0, cameraToOcean1);
                float oceanRayLength = min(cameraToOcean1 - cameraToOcean0, depth - cameraToOcean0);

                if (oceanRayLength > 0) {
                    float3 surfacePoint = rayOrigin + rayDirection * depth;
                    float3 upDirection = normalize(surfacePoint - _PlanetCenter);
                    float surfaceToOcean0;
                    float surfaceToOcean1;
                    raySphereIntersect(surfacePoint, upDirection, _PlanetCenter, _OceanRadius, surfaceToOcean0, surfaceToOcean1);
                    surfaceToOcean0 = max(0, surfaceToOcean0);
                    surfaceToOcean1 = max(0, surfaceToOcean1);
                    float sunRayLength = surfaceToOcean1 - surfaceToOcean0;

                    float3 positionWS = rayOrigin + rayDirection * cameraToOcean0;
                    float3 normalWS = normalize(positionWS - _PlanetCenter);

                    #if defined(_TRIPLANAR_MAPPING)
                        float3 positionOS = TransformWorldToObject(positionWS);
                        float3 normalOS = TransformWorldToObjectDir(normalWS);
                        float3 bf = pow(abs(normalOS), _TriplanarSharpness);
                        bf /= bf.x + bf.y + bf.z;
                        float2 tx = positionOS.zy * _TriplanarMapScale;
                        float2 ty = positionOS.xz * _TriplanarMapScale;
                        float2 tz = positionOS.xy * _TriplanarMapScale;

                        float3 cnx = SampleNormal(tx * _WaveScaleA + _Time.x * _WaveVelocityA, TEXTURE2D_ARGS(_WaveNormalMapA, sampler_WaveNormalMapA), _WaveStrengthA) * bf.x;
                        float3 cny = SampleNormal(ty * _WaveScaleA + _Time.x * _WaveVelocityA, TEXTURE2D_ARGS(_WaveNormalMapA, sampler_WaveNormalMapA), _WaveStrengthA) * bf.y;
                        float3 cnz = SampleNormal(tz * _WaveScaleA + _Time.x * _WaveVelocityA, TEXTURE2D_ARGS(_WaveNormalMapA, sampler_WaveNormalMapA), _WaveStrengthA) * bf.z;
                        float3 dnx = SampleNormal(tx * _WaveScaleB + _Time.x * _WaveVelocityB, TEXTURE2D_ARGS(_WaveNormalMapB, sampler_WaveNormalMapB), _WaveStrengthB) * bf.x;
                        float3 dny = SampleNormal(ty * _WaveScaleB + _Time.x * _WaveVelocityB, TEXTURE2D_ARGS(_WaveNormalMapB, sampler_WaveNormalMapB), _WaveStrengthB) * bf.y;
                        float3 dnz = SampleNormal(tz * _WaveScaleB + _Time.x * _WaveVelocityB, TEXTURE2D_ARGS(_WaveNormalMapB, sampler_WaveNormalMapB), _WaveStrengthB) * bf.z;
                        float3 enx = SampleNormal(tx * _WaveScaleC + _Time.x * _WaveVelocityC, TEXTURE2D_ARGS(_WaveNormalMapC, sampler_WaveNormalMapC), _WaveStrengthC) * bf.x;
                        float3 eny = SampleNormal(ty * _WaveScaleC + _Time.x * _WaveVelocityC, TEXTURE2D_ARGS(_WaveNormalMapC, sampler_WaveNormalMapC), _WaveStrengthC) * bf.y;
                        float3 enz = SampleNormal(tz * _WaveScaleC + _Time.x * _WaveVelocityC, TEXTURE2D_ARGS(_WaveNormalMapC, sampler_WaveNormalMapC), _WaveStrengthC) * bf.z;
                        
                        float3 normalTS = (cnx + cny + cnz + dnx + dny + dnz + enx + eny + enz) / 3;
                        float3 binormal = cross(normalOS, normalTS);
                        float3 tangent = cross(normalOS, binormal);

                        normalOS = normalize(
		                    normalTS.x * tangent +
		                    normalTS.y * binormal +
		                    normalTS.z * normalOS
                        );
                        normalWS = TransformObjectToWorldDir(normalOS);
                    #endif

                    float3 halfVec = SafeNormalize(sunDirection - rayDirection);
                    float NdotH = saturate(dot(normalWS, halfVec));
                    float specular = pow(NdotH, _Smoothness);
                    specular *= smoothstep(0, 1, distance(rayOrigin, _PlanetCenter) - _OceanRadius);
                    float diffuse = saturate(dot(normalWS, sunDirection));

                    float opticalDepth = oceanRayLength + sunRayLength;
                    float opticalDepth01 = 1 - exp(-opticalDepth * _DepthMultiplier);
                    float alpha = 1 - exp(-opticalDepth * _AlphaMultiplier);
                    float4 oceanColor = diffuse * lerp(_ShallowColor, _DeepColor, opticalDepth01);
                    
                    return lerp(color, oceanColor, alpha) + specular;
                }

                return color;
            }

            ENDHLSL
        }
    }
}
