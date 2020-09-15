using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// using UnityEngine.Experimental.Rendering;

public class Blit : ScriptableRendererFeature
{
    [System.Serializable]
    public class BlitSettings
    {
        public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

        public Material blitMaterial = null;
        public int blitMaterialPassIndex = -1;
        public Target destination = Target.Color;
        public string textureId = "_BlitPassTexture";
    }

    public enum Target
    {
        Color,
        Texture
    }

    class BlitPass : ScriptableRenderPass
    {
        public enum RenderTarget
        {
            Color,
            RenderTexture,
        }

        public Material blitMaterial = null;
        public int blitShaderPassIndex = 0;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source { get; set; }
        private RenderTargetHandle destination { get; set; }
        private RenderTargetIdentifier depth { get; set; }

        RenderTargetHandle temporaryColorTexture;
        string profilerTag;

        public BlitPass(RenderPassEvent renderPassEvent, Material blitMaterial, int blitShaderPassIndex, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            this.blitMaterial = blitMaterial;
            this.blitShaderPassIndex = blitShaderPassIndex;
            profilerTag = tag;
            temporaryColorTexture.Init("_TemporaryColorTexture");
        }

        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, RenderTargetIdentifier depth)
        {
            this.source = source;
            this.destination = destination;
            this.depth = depth;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 32;

            // Can't read and write to same color target, create a temp render target to blit. 
            if (destination == RenderTargetHandle.CameraTarget)
            {
                // cmd.SetGlobalTexture("_CameraDepthAttachment", depth);
                cmd.GetTemporaryRT(temporaryColorTexture.id, opaqueDesc, filterMode);
                Blit(cmd, source, temporaryColorTexture.Identifier(), blitMaterial, blitShaderPassIndex);
                Blit(cmd, temporaryColorTexture.Identifier(), source);
            }
            else
            {
                Blit(cmd, source, destination.Identifier(), blitMaterial, blitShaderPassIndex);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (destination == RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(temporaryColorTexture.id);
            }
        }
    }

    public BlitSettings settings = new BlitSettings();
    RenderTargetHandle renderTextureHandle;
    BlitPass blitPass;

    public override void Create()
    {
        var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
        settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
        blitPass = new BlitPass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name);
        renderTextureHandle.Init(settings.textureId);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera) {
            return;
        }

        var src = renderer.cameraColorTarget;
        var depth = renderer.cameraDepth;
        var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : renderTextureHandle;

        if (settings.blitMaterial == null)
        {
            Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
            return;
        }

        blitPass.Setup(src, dest, depth);
        renderer.EnqueuePass(blitPass);
    }
}
