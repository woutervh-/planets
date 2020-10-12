Shader "CustomRenderPipeline/CopyDepth" {
    SubShader {
        Tags { "RenderType" = "Opaque" }
        ZTest Always ZWrite On Cull Off ColorMask 0

        Pass {
            Name "CopyDepth"

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            sampler2D _CameraDepthTexture1;
            sampler2D _CameraDepthTexture2;
            sampler2D _CameraDepthTexture3;
            float4 _ZBufferParams1;
            float4 _ZBufferParams2;
            float4 _ZBufferParams3;

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
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
                return output;
            }

            float MyLinearEyeDepth(float z, float4 _ZBufferParams) {
                return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
            }

            float MyInverseLinearEyeDepth(float depth, float4 _ZBufferParams) {
                return 1.0 / (_ZBufferParams.z * depth) + _ZBufferParams.w;
            }

            float Frag(Varyings input) : SV_Depth {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float depth1 = tex2D(_CameraDepthTexture1, input.uv).r;
                float depth2 = tex2D(_CameraDepthTexture2, input.uv).r;
                float depth3 = tex2D(_CameraDepthTexture3, input.uv).r;
                depth1 = MyLinearEyeDepth(depth1, _ZBufferParams1) * ceil(depth1) * (1 - ceil(depth2)) * (1 - ceil(depth3));
                depth2 = MyLinearEyeDepth(depth2, _ZBufferParams2) * ceil(depth2) * (1 - ceil(depth3));
                depth3 = MyLinearEyeDepth(depth3, _ZBufferParams3) * ceil(depth3);
                return MyInverseLinearEyeDepth(depth1 + depth2 + depth3, _ZBufferParams);
            }

            ENDHLSL
        }
    }
}
