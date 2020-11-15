Shader "SQT/Ocean" {
    Properties {
        [MainColor] _BaseColor ("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        _Metallic ("Metallic", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }

    SubShader {
        Tags { "RenderType" = "Transparent" "Queue"="Transparent" }

        Pass {
            Name "Lit"

            HLSLPROGRAM

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

            sampler2D _CameraColorTexture;

            struct FragOutput {
                float4 color: SV_Target;
            };

            Varyings Vertex(Attributes input) {
                return LitPassVertex(input);
            }

            FragOutput Fragment(Varyings input) {
                float3 positionOS = TransformWorldToObject(input.positionWS);
                float3 normalOS = TransformWorldToObjectDir(input.normalWS);
                // float4 positionCS = input.positionCS;
                // float2 uv = positionCS.xy / positionCS.w / 2 + 0.5;

                // float4 color = tex2D(_CameraColorTexture, uv);

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                FragOutput output;
                output.color = UniversalFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
                output.color.rgb = MixFog(output.color.rgb, inputData.fogCoord);
                // output.color = color;
                return output;
            }

            ENDHLSL
        }
    }
}
