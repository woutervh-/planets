using UnityEngine;

namespace CustomRenderPipeline
{
    [CreateAssetMenu(menuName = "Custom Render Pipeline/Post-processing Settings")]
    public class PostProcessingSettings : ScriptableObject
    {
        public Material material = null;
    }
}
