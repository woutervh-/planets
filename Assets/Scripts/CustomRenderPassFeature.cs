using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// using UnityEngine.Experimental.Rendering;

public class CustomRenderPassFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public struct CustomRenderPassSettings
    {
        public Material blitMaterial;
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        private CustomRenderPassSettings settings;

        public CustomRenderPass(CustomRenderPassSettings settings)
        {
            this.settings = settings;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // cmd.GetTemporaryRT(destination.id, cameraTextureDescriptor, FilterMode.Point);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // renderingData.cameraData.cameraTargetDescriptor
            //  RenderTargetHandle source;

            CommandBuffer cmd = CommandBufferPool.Get("CustomRenderPass");

            cmd.SetGlobalTexture("_BlitTex", RenderTargetHandle.CameraTarget.id);
            // cmd.SetGlobalTexture("_BlitTex", renderingData.cameraData.targetTexture);
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(renderingData.cameraData.camera.pixelRect);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, settings.blitMaterial);
            cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// Cleanup any allocated resources that were created during the execution of this render pass.
        public override void FrameCleanup(CommandBuffer cmd)
        {
        }
    }

    public CustomRenderPassSettings settings;
    CustomRenderPass scriptablePass;

    public override void Create()
    {
        scriptablePass = new CustomRenderPass(settings);

        // Configures where the render pass should be injected.
        scriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(scriptablePass);
    }
}
