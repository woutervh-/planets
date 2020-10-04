using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRenderPipeline
{
    public class PostProcessing
    {
        public static void Render(CommandBuffer buffer, int sourceId)
        {
            buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
        }
    }
}
