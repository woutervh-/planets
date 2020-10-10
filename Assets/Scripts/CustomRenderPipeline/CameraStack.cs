using UnityEngine;

namespace CustomRenderPipeline
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class CameraStack : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        Camera primaryCamera;
        [SerializeField]
        [HideInInspector]
        CameraClearFlags originalClearFlags;
        [SerializeField]
        [HideInInspector]
        Color originalBackgroundColor;
        [SerializeField]
        [HideInInspector]
        Camera secondaryCamera;
        [SerializeField]
        [HideInInspector]
        Camera tertiaryCamera;
        [SerializeField]
        [HideInInspector]
        GameObject secondaryGameObject;
        [SerializeField]
        [HideInInspector]
        GameObject tertiaryGameObject;
        RenderTexture secondaryRenderTexture;
        RenderTexture tertiaryRenderTexture;

        public Camera SecondaryCamera
        {
            get
            {
                return secondaryCamera;
            }
        }

        public Camera TertiaryCamera
        {
            get
            {
                return tertiaryCamera;
            }
        }

        public RenderTexture GetSecondaryRenderTexture()
        {
            return secondaryRenderTexture;
        }

        public RenderTexture GetTertiaryRenderTexture()
        {
            return tertiaryRenderTexture;
        }

        void OnEnable()
        {
            if (primaryCamera == null)
            {
                primaryCamera = GetComponent<Camera>();
                originalClearFlags = primaryCamera.clearFlags;
                originalBackgroundColor = primaryCamera.backgroundColor;
                primaryCamera.clearFlags = CameraClearFlags.Color;
                primaryCamera.backgroundColor = Color.clear;
            }
            if (secondaryGameObject == null)
            {
                secondaryGameObject = new GameObject(primaryCamera.name + " (secondary)");
                secondaryGameObject.transform.parent = transform;
            }
            if (tertiaryGameObject == null)
            {
                tertiaryGameObject = new GameObject(primaryCamera.name + " (tertiary)");
                tertiaryGameObject.transform.parent = transform;
            }
            if (secondaryCamera == null)
            {
                secondaryCamera = secondaryGameObject.AddComponent<Camera>();
                secondaryCamera.CopyFrom(primaryCamera);
                secondaryCamera.depth = primaryCamera.depth - 1;
                secondaryCamera.clearFlags = CameraClearFlags.Color;
                secondaryCamera.backgroundColor = Color.clear;
            }
            if (tertiaryCamera == null)
            {
                tertiaryCamera = tertiaryGameObject.AddComponent<Camera>();
                tertiaryCamera.CopyFrom(primaryCamera);
                tertiaryCamera.depth = secondaryCamera.depth - 1;
                tertiaryCamera.clearFlags = originalClearFlags;
                tertiaryCamera.backgroundColor = originalBackgroundColor;
            }
            if (secondaryRenderTexture != null)
            {
                secondaryRenderTexture.Release();
            }
            secondaryRenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
            if (tertiaryRenderTexture != null)
            {
                tertiaryRenderTexture.Release();
            }
            tertiaryRenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
            if (Screen.width >= 1 && Screen.height >= 1)
            {
                // secondaryCamera.targetTexture = secondaryRenderTexture;
                // tertiaryCamera.targetTexture = tertiaryRenderTexture;
            }
        }

        void Update()
        {
            if (secondaryRenderTexture.width != Screen.width || secondaryRenderTexture.height != Screen.height)
            {
                secondaryRenderTexture.Release();
                secondaryRenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
                // secondaryCamera.targetTexture = secondaryRenderTexture;
            }
            if (tertiaryRenderTexture.width != Screen.width || tertiaryRenderTexture.height != Screen.height)
            {
                tertiaryRenderTexture.Release();
                tertiaryRenderTexture = new RenderTexture(Screen.width, Screen.height, 16);
                // tertiaryCamera.targetTexture = tertiaryRenderTexture;
            }

            secondaryCamera.fieldOfView = primaryCamera.fieldOfView;
            tertiaryCamera.fieldOfView = primaryCamera.fieldOfView;

            secondaryCamera.nearClipPlane = primaryCamera.farClipPlane;
            secondaryCamera.farClipPlane = primaryCamera.farClipPlane * primaryCamera.farClipPlane / primaryCamera.nearClipPlane;

            tertiaryCamera.nearClipPlane = secondaryCamera.farClipPlane;
            tertiaryCamera.farClipPlane = secondaryCamera.farClipPlane * secondaryCamera.farClipPlane / secondaryCamera.nearClipPlane;
        }

        void OnDisable()
        {
            if (secondaryRenderTexture != null)
            {
                secondaryRenderTexture.Release();
            }
            if (tertiaryRenderTexture != null)
            {
                tertiaryRenderTexture.Release();
            }
            primaryCamera.clearFlags = originalClearFlags;
            primaryCamera.backgroundColor = originalBackgroundColor;
            DestroyImmediate(secondaryGameObject);
            DestroyImmediate(tertiaryGameObject);
            primaryCamera = null;
            secondaryGameObject = null;
            tertiaryGameObject = null;
            secondaryCamera = null;
            tertiaryCamera = null;
            secondaryRenderTexture = null;
            tertiaryRenderTexture = null;
        }
    }
}
