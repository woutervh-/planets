Shader "Atmosphere" {
    Properties {
        _PlanetRadius ("Planet radius", Float) = 0.5
        _Blend ("Blend", Float) = 1
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZTest Always ZWrite Off Cull Off

        Pass {
            Name "Atmosphere"

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D_X(_CameraColorTexture);
            // TEXTURE2D(_BlitTex);
            float _Blend;

            float4 Frag(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 color = SAMPLE_TEXTURE2D(_CameraColorTexture, sampler_PointClamp, UnityStereoTransformScreenSpaceTex(input.uv)).xyz;
                float luminance = dot(color, float3(0.2126729, 0.715122, 0.0721750));
                color = lerp(color, luminance.xxx, _Blend.xxx);

                // return float4(0.2126729, 0.715122, 0.0721750, 1);

                return float4(color, 1);
            }

            ENDHLSL
        }
    }
}
