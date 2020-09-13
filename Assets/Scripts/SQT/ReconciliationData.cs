using UnityEngine;

namespace SQT
{
    public class ReconciliationData
    {
        public Context.Branch branch;
        public float desiredLength;
        public Vector2 pointInPlane;

        public static ReconciliationData GetData(Context context, Camera camera)
        {
            Vector3 sphereToCamera = context.constants.gameObject.transform.InverseTransformPoint(camera.transform.position);
            float distanceToSphere = Mathf.Abs(Mathf.Sqrt(Vector3.Dot(sphereToCamera, sphereToCamera)) - 1f);
            Vector3 aa = camera.transform.position + camera.transform.forward * distanceToSphere;
            Vector3 a = camera.WorldToScreenPoint(aa);
            Vector3 b = new Vector3(a.x, a.y + context.constants.desiredScreenSpaceLength, a.z);
            Vector3 bb = camera.ScreenToWorldPoint(b);
            float desiredLength = (aa - bb).magnitude;

            for (int i = 0; i < 6; i++)
            {
                Vector2? pointInPlane = GetPointInPlane(context.branches[i], camera, sphereToCamera);
                if (pointInPlane != null)
                {
                    return new ReconciliationData
                    {
                        branch = context.branches[i],
                        desiredLength = desiredLength,
                        pointInPlane = pointInPlane.Value
                    };
                }
            }

            return null;
        }

        static Vector2? GetPointInPlane(Context.Branch branch, Camera camera, Vector3 sphereToCamera)
        {
            Vector3 direction;
            float denominator;
            if (sphereToCamera.sqrMagnitude == 0f)
            {
                // Camera is at the center of the object.
                direction = branch.up;
                denominator = 1f;
            }
            else
            {
                direction = sphereToCamera.normalized;
                denominator = Vector3.Dot(branch.up, direction);

                if (denominator <= 0f)
                {
                    // Camera is in opposite hemisphere.
                    return null;
                }
            }

            Vector3 pointOnPlane = direction / denominator;
            Vector2 pointInPlane = new Vector2(Vector3.Dot(branch.forward, pointOnPlane), Vector3.Dot(branch.right, pointOnPlane));

            if (pointInPlane.x < -1f || 1f < pointInPlane.x || pointInPlane.y < -1f || 1f < pointInPlane.y)
            {
                return null;
            }

            return pointInPlane;
        }
    }
}
