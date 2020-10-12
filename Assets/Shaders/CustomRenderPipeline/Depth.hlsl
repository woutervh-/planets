#ifndef CUSTOM_RENDER_PIPELINE_DEPTH
#define CUSTOM_RENDER_PIPELINE_DEPTH

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#if defined(_MULTIPLE_DEPTH_TEXTURES)
    sampler2D _CameraDepthTexture1;
    sampler2D _CameraDepthTexture2;
    sampler2D _CameraDepthTexture3;
    float4 _ZBufferParams1;
    float4 _ZBufferParams2;
    float4 _ZBufferParams3;

    float GetLinearEyeDepth(float2 uv) {
        float depth1 = tex2D(_CameraDepthTexture1, uv).r;
        float depth2 = tex2D(_CameraDepthTexture2, uv).r;
        float depth3 = tex2D(_CameraDepthTexture3, uv).r;

        if (depth1 > 0) {
            return LinearEyeDepth(depth1, _ZBufferParams1);
        } else {
            if (depth2 > 0) {
                return LinearEyeDepth(depth2, _ZBufferParams2);
            } else {
                return LinearEyeDepth(depth3, _ZBufferParams3);
            }
        }
    }
#else
    sampler2D _CameraDepthTexture;

    float GetLinearEyeDepth(float2 uv) {
        float depth = tex2D(_CameraDepthTexture, uv).r;
        return LinearEyeDepth(depth, _ZBufferParams);
    }
#endif

#endif
