using UnityEngine;

namespace SQT
{
    public class Context
    {
        public class Constants
        {
            public int maxDepth;
            public int resolution;
            public float desiredScreenSpaceLength;
            public Material material;
            public GameObject gameObject;
            public MeshModifier meshModifier;
        }

        public class Branch
        {
            public int index;
            public Vector3 up;
            public Vector3 forward;
            public Vector3 right;
            public GameObject gameObject;

            public static Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

            public static Vector3 GetForward(Vector3 up)
            {
                return new Vector3(up.y, up.z, up.x);
            }

            public static Vector3 GetRight(Vector3 up)
            {
                return Vector3.Cross(up, GetForward(up));
            }

            public static Branch[] GetFromConstants(Constants constants)
            {
                Branch[] branches = new Branch[6];
                for (int i = 0; i < 6; i++)
                {
                    GameObject gameObject = new GameObject("Branch (" + i + ")");
                    gameObject.transform.SetParent(constants.gameObject.transform, false);
                    branches[i] = new Branch
                    {
                        up = directions[i],
                        forward = GetForward(directions[i]),
                        right = GetRight(directions[i]),
                        index = i,
                        gameObject = gameObject
                    };
                }
                return branches;
            }
        }

        public class Depth
        {
            public int index;
            public float scale;
            public float approximateSize;

            public static Depth[] GetFromConstants(Constants constants)
            {
                Depth[] depths = new Depth[constants.maxDepth + 1];
                for (int i = 0; i <= constants.maxDepth; i++)
                {
                    float scale = GetScale(i);
                    float approximateSize = scale / constants.resolution;

                    depths[i] = new Depth
                    {
                        index = i,
                        scale = scale,
                        approximateSize = approximateSize
                    };
                }
                return depths;
            }

            public static float GetScale(int depth)
            {
                return 1f / Mathf.Pow(2f, depth);
            }
        }

        public class Triangles
        {
            public int[] triangles;

            public static Triangles[] GetFromConstants(Constants constants)
            {
                Triangles[] triangles = new Triangles[17];
                for (int i = 0; i < 17; i++)
                {
                    triangles[i] = new Triangles
                    {
                        triangles = MeshHelper.GetTriangles(constants.resolution, i)
                    };
                }
                return triangles;
            }
        }

        public Constants constants;
        public Branch[] branches;
        public Depth[] depths;
        public Triangles[] triangles;
        public Node[] roots;
    }
}
