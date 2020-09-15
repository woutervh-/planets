using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Atmosphere
{
    public class Atmosphere : ScriptableRendererFeature
    {
        [System.Serializable]
        public class AtmosphereSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;

            public Material atmosphereMaterial = null;
            public int atmosphereMaterialPassIndex = -1;
            public Target destination = Target.Color;
            public string textureId = "_AtmospherePassTexture";
        }

        public enum Target
        {
            Color,
            Texture
        }

        class AtmospherePass : ScriptableRenderPass
        {
            public enum RenderTarget
            {
                Color,
                RenderTexture,
            }

            public Material atmosphereMaterial = null;
            public int atmosphereShaderPassIndex = 0;
            public FilterMode filterMode { get; set; }

            private RenderTargetIdentifier source { get; set; }
            private RenderTargetHandle destination { get; set; }

            RenderTargetHandle temporaryColorTexture;
            string profilerTag;

            public AtmospherePass(RenderPassEvent renderPassEvent, Material atmosphereMaterial, int atmosphereShaderPassIndex, string tag)
            {
                this.renderPassEvent = renderPassEvent;
                this.atmosphereMaterial = atmosphereMaterial;
                this.atmosphereShaderPassIndex = atmosphereShaderPassIndex;
                profilerTag = tag;
                temporaryColorTexture.Init("_TemporaryColorTexture");
            }

            public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
            {
                this.source = source;
                this.destination = destination;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

                RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
                opaqueDesc.depthBufferBits = 0;

                // Can't read and write to same color target, create a temp render target to blit. 
                if (destination == RenderTargetHandle.CameraTarget)
                {
                    cmd.GetTemporaryRT(temporaryColorTexture.id, opaqueDesc, filterMode);
                    Blit(cmd, source, temporaryColorTexture.Identifier(), atmosphereMaterial, atmosphereShaderPassIndex);
                    Blit(cmd, temporaryColorTexture.Identifier(), source);
                }
                else
                {
                    Blit(cmd, source, destination.Identifier(), atmosphereMaterial, atmosphereShaderPassIndex);
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

        public AtmosphereSettings settings = new AtmosphereSettings();
        RenderTargetHandle renderTextureHandle;
        AtmospherePass atmospherePass;

        public override void Create()
        {
            var passIndex = settings.atmosphereMaterial != null ? settings.atmosphereMaterial.passCount - 1 : 1;
            settings.atmosphereMaterialPassIndex = Mathf.Clamp(settings.atmosphereMaterialPassIndex, -1, passIndex);
            atmospherePass = new AtmospherePass(settings.Event, settings.atmosphereMaterial, settings.atmosphereMaterialPassIndex, name);
            renderTextureHandle.Init(settings.textureId);

            Texture2D opticalDepth = new Texture2D(256, 256);

        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
            {
                return;
            }

            var src = renderer.cameraColorTarget;
            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : renderTextureHandle;

            if (settings.atmosphereMaterial == null)
            {
                Debug.LogWarningFormat("Missing Atmosphere Material. {0} atmosphere pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
                return;
            }

            atmospherePass.Setup(src, dest);
            renderer.EnqueuePass(atmospherePass);
        }
    }
}
