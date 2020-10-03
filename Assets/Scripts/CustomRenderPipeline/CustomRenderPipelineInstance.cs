using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipelineInstance : RenderPipeline
{
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        // This is where you can write custom rendering code. Customize this method to customize your SRP.

        foreach (Camera camera in cameras) {
			context.SetupCameraProperties(camera);
		}

        // CommandBuffer cmd = new CommandBuffer();
        // cmd.ClearRenderTarget(true, true, Color.black);
        // context.ExecuteCommandBuffer(cmd);
        // cmd.Release();

        // Tell the Scriptable Render Context to tell the graphics API to perform the scheduled commands.
        context.Submit();
    }
}
