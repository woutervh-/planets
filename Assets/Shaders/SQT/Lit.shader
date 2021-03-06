Shader "SQT/Lit" {
    Properties {
        [MainColor] _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Toggle(_NORMALMAP)] _NormalMap ("Bump", Float) = 0
        _BumpScale ("Bump scale", Float) = 1.0
        _BumpMap ("Bump map", 2D) = "bump" {}

        [Toggle(_TRIPLANAR_MAPPING)] _TriplanarMapping ("Triplanar mapping", Float) = 0
        _TriplanarMapScale ("Triplanar map scale", Float) = 1.0
        _TriplanarSharpness ("Triplanar sharpness", Float) = 1.0

        [Toggle(_VERTEX_DISPLACEMENT)] _VertexDisplacement ("Vertex displacement", Float) = 0
        [Toggle(_PER_FRAGMENT_HEIGHT)] _PerFragmentHeight ("Per fragment height", Float) = 0
        [Toggle(_PER_FRAGMENT_NORMALS)] _PerFragmentNormals ("Per fragment normals", Float) = 0
        [Toggle(_FINITE_DIFFERENCE_NORMALS)] _FiniteDifferenceNormals ("Finite difference normals", Float) = 0

        [HideInInspector] _Gradients2D ("Gradients", 2D) = "white" {}
        [HideInInspector] _Permutation2D ("Permutation", 2D) = "white" {}
        [HideInInspector] _Strength ("Strength", Float) = 1
        [HideInInspector] _Frequency ("Frequency", Float) = 1
        [HideInInspector] _Lacunarity ("Lacunarity", Float) = 2
        [HideInInspector] _Persistence ("Persistence", Float) = 0.5
        [HideInInspector] _Octaves ("Octaves", Int) = 8
    }

    SubShader {
        Pass {
            Name "Lit"

            HLSLPROGRAM

            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _VERTEX_DISPLACEMENT
            #pragma shader_feature _PER_FRAGMENT_NORMALS
            #pragma shader_feature _PER_FRAGMENT_HEIGHT
            #pragma shader_feature _FINITE_DIFFERENCE_NORMALS
            #pragma shader_feature _TRIPLANAR_MAPPING

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #pragma vertex Vertex
            #pragma fragment Fragment

            #define _ADDITIONAL_LIGHTS // This is to include positionWS in Varyings.

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            #include "../Noise.hlsl"

            #ifdef _TRIPLANAR_MAPPING
                float _TriplanarMapScale;
                float _TriplanarSharpness;
            #endif

            struct FragOutput {
                float4 color: SV_Target;
                float depth: SV_Depth;
            };

            Varyings Vertex(Attributes input) {
                #if defined(_VERTEX_DISPLACEMENT) || defined(_PER_FRAGMENT_NORMALS)
                    float3 pointOnUnitSphere = normalize(input.positionOS.xyz);
                    float4 noiseSample = noise(pointOnUnitSphere);

                    #if defined(_VERTEX_DISPLACEMENT)
                        input.positionOS.xyz = pointOnUnitSphere * (1 + noiseSample.w);
                    #endif

                    #if defined(_PER_FRAGMENT_NORMALS)
                        input.normalOS = pointOnUnitSphere;
                    #else
                        input.normalOS = normalize(pointOnUnitSphere - noiseSample.xyz);
                    #endif
                #endif

                return LitPassVertex(input);
            }

            FragOutput Fragment(Varyings input) {
                float3 positionOS = TransformWorldToObject(input.positionWS);
                float3 pointOnUnitSphere = normalize(positionOS);

                #if defined(_PER_FRAGMENT_NORMALS) || defined(_PER_FRAGMENT_HEIGHT)
                    float4 noiseSample = noise(pointOnUnitSphere);
                #endif

                #if defined(_PER_FRAGMENT_HEIGHT)
                    positionOS = pointOnUnitSphere * (1 + noiseSample.w);
                    float4 positionCS = TransformWorldToHClip(TransformObjectToWorld(positionOS));
                    positionCS.z /= positionCS.w;
                #else
                    float4 positionCS = input.positionCS;
                #endif

                #if defined(_PER_FRAGMENT_NORMALS)
                    #if defined(_FINITE_DIFFERENCE_NORMALS)
                        #ifdef UNITY_REVERSED_Z
                            float h = 1.0 - input.positionCS.z;
                        #else
                            float h = input.positionCS.z;
                        #endif
                        h /= 8.0;
                        h *= h;
                        float3 normalOS = normalize(pointOnUnitSphere - finiteDifferenceGradient(pointOnUnitSphere, h));
                    #else
                        float3 normalOS = normalize(pointOnUnitSphere - noiseSample.xyz);
                    #endif

                    input.normalWS.xyz = TransformObjectToWorldNormal(normalOS);
                #else
                    float3 normalOS = TransformWorldToObjectDir(input.normalWS);
                #endif

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);

                #if defined(_TRIPLANAR_MAPPING)
                    float3 bf = pow(abs(normalOS), _TriplanarSharpness);
                    bf /= bf.x + bf.y + bf.z;
                    float2 tx = positionOS.zy * _TriplanarMapScale;
                    float2 ty = positionOS.xz * _TriplanarMapScale;
                    float2 tz = positionOS.xy * _TriplanarMapScale;

                    float4 cx = SampleAlbedoAlpha(tx, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * bf.x;
                    float4 cy = SampleAlbedoAlpha(ty, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * bf.y;
                    float4 cz = SampleAlbedoAlpha(tz, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * bf.z;
                    surfaceData.albedo = _BaseColor.rgb * (cx + cy + cz).rgb;

                    #ifdef _NORMALMAP
                        float3 cnx = SampleNormal(tx, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale) * bf.x;
                        float3 cny = SampleNormal(ty, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale) * bf.y;
                        float3 cnz = SampleNormal(tz, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale) * bf.z;
                        surfaceData.normalTS = cnx + cny + cnz;
                    #endif
                #endif

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                FragOutput output;
                output.color = UniversalFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
                output.color.rgb = MixFog(output.color.rgb, inputData.fogCoord);
                output.depth = clamp(positionCS.z, FLT_MIN, 1);
                return output;
            }

            ENDHLSL
        }
    }
}
