using UnityEngine;

namespace SQT
{
    public static class MeshHelper
    {
        public static void GenerateVertices(Context context, Node node, out Vector3[] positions, out Vector3[] normals)
        {
            positions = new Vector3[context.constants.resolution * context.constants.resolution];
            normals = new Vector3[context.constants.resolution * context.constants.resolution];

            Vector3 origin = node.branch.up + node.offset.x * node.branch.forward + node.offset.y * node.branch.right;
            for (int y = 0; y < context.constants.resolution; y++)
            {
                for (int x = 0; x < context.constants.resolution; x++)
                {
                    int vertexIndex = x + context.constants.resolution * y;
                    Vector2 percent = new Vector2(x, y) / (context.constants.resolution - 1);
                    Vector3 pointOnUnitCube = origin
                        + Mathf.Lerp(-1f, 1f, percent.x) * node.depth.scale * node.branch.forward
                        + Mathf.Lerp(-1f, 1f, percent.y) * node.depth.scale * node.branch.right;

                    Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                    positions[vertexIndex] = pointOnUnitSphere;
                    normals[vertexIndex] = pointOnUnitSphere;
                }
            }
        }

        public static int[] GetTriangles(int resolution, int neighborMask)
        {
            int lerpWest = (neighborMask & 1) == 0 ? 0 : 1;
            int lerpEast = (neighborMask & 2) == 0 ? 0 : 1;
            int lerpSouth = (neighborMask & 4) == 0 ? 0 : 1;
            int lerpNorth = (neighborMask & 8) == 0 ? 0 : 1;

            int outerTrianglesCount = 0;
            if (lerpWest == 1)
            {
                outerTrianglesCount += (resolution / 2 + 2 * (resolution / 2 - 1));
                outerTrianglesCount += 2;
                outerTrianglesCount -= lerpSouth;
                outerTrianglesCount -= lerpNorth;
            }
            if (lerpEast == 1)
            {
                outerTrianglesCount += (resolution / 2 + 2 * (resolution / 2 - 1));
                outerTrianglesCount += 2;
                outerTrianglesCount -= lerpSouth;
                outerTrianglesCount -= lerpNorth;
            }
            if (lerpSouth == 1)
            {
                outerTrianglesCount += (resolution / 2 + 2 * (resolution / 2 - 1));
                outerTrianglesCount += 2;
                outerTrianglesCount -= lerpEast;
                outerTrianglesCount -= lerpWest;
            }
            if (lerpNorth == 1)
            {
                outerTrianglesCount += (resolution / 2 + 2 * (resolution / 2 - 1));
                outerTrianglesCount += 2;
                outerTrianglesCount -= lerpEast;
                outerTrianglesCount -= lerpWest;
            }

            int innerTrianglesCount = (resolution - lerpWest - lerpEast - 1) * (resolution - lerpSouth - lerpNorth - 1) * 2;
            int[] triangles = new int[(innerTrianglesCount + outerTrianglesCount) * 3];
            int triangleIndex = 0;

            // Inner triangles.
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int vertexIndex = x + resolution * y;
                    if (lerpWest <= x && x < resolution - lerpEast - 1 && lerpSouth <= y && y < resolution - lerpNorth - 1)
                    {
                        triangles[triangleIndex] = vertexIndex;
                        triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution;
                        triangles[triangleIndex + 3] = vertexIndex;
                        triangles[triangleIndex + 4] = vertexIndex + 1;
                        triangles[triangleIndex + 5] = vertexIndex + resolution + 1;
                        triangleIndex += 6;
                    }
                }
            }

            if (lerpWest == 1)
            {
                for (int y = 0; y < resolution - 2; y++)
                {
                    int vertexIndex = y * resolution;
                    if (y % 2 == 0)
                    {
                        triangles[triangleIndex] = vertexIndex;
                        triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution + resolution;
                        triangleIndex += 3;
                    }
                    else
                    {
                        triangles[triangleIndex] = vertexIndex + 1;
                        triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution;
                        triangles[triangleIndex + 3] = vertexIndex + resolution;
                        triangles[triangleIndex + 4] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 5] = vertexIndex + resolution + resolution + 1;
                        triangleIndex += 6;
                    }
                }
                if (lerpSouth == 0)
                {
                    int vertexIndex = 0;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution + 1;
                    triangleIndex += 3;
                }
                if (lerpNorth == 0)
                {
                    int vertexIndex = resolution * (resolution - 2);
                    triangles[triangleIndex] = vertexIndex + 1;
                    triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution;
                    triangleIndex += 3;
                }
            }

            if (lerpEast == 1)
            {
                for (int y = 0; y < resolution - 2; y++)
                {
                    int vertexIndex = y * resolution + resolution - 2;
                    if (y % 2 == 0)
                    {
                        triangles[triangleIndex] = vertexIndex + 1;
                        triangles[triangleIndex + 1] = vertexIndex + resolution + resolution + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution;
                        triangleIndex += 3;
                    }
                    else
                    {
                        triangles[triangleIndex] = vertexIndex;
                        triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution;
                        triangles[triangleIndex + 3] = vertexIndex + resolution;
                        triangles[triangleIndex + 4] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 5] = vertexIndex + resolution + resolution;
                        triangleIndex += 6;
                    }
                }
                if (lerpSouth == 0)
                {
                    int vertexIndex = resolution - 2;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution;
                    triangleIndex += 3;
                }
                if (lerpNorth == 0)
                {
                    int vertexIndex = resolution * (resolution - 2) + resolution - 2;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution;
                    triangleIndex += 3;
                }
            }

            if (lerpSouth == 1)
            {
                for (int x = 0; x < resolution - 2; x++)
                {
                    int vertexIndex = x;
                    if (x % 2 == 0)
                    {
                        triangles[triangleIndex] = vertexIndex;
                        triangles[triangleIndex + 1] = vertexIndex + 2;
                        triangles[triangleIndex + 2] = vertexIndex + resolution + 1;
                        triangleIndex += 3;
                    }
                    else
                    {
                        triangles[triangleIndex] = vertexIndex + resolution;
                        triangles[triangleIndex + 1] = vertexIndex + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 3] = vertexIndex + 1;
                        triangles[triangleIndex + 4] = vertexIndex + resolution + 2;
                        triangles[triangleIndex + 5] = vertexIndex + resolution + 1;
                        triangleIndex += 6;
                    }
                }
                if (lerpWest == 0)
                {
                    int vertexIndex = 0;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution;
                    triangleIndex += 3;
                }
                if (lerpEast == 0)
                {
                    int vertexIndex = resolution - 2;
                    triangles[triangleIndex] = vertexIndex + 1;
                    triangles[triangleIndex + 1] = vertexIndex + resolution + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution;
                    triangleIndex += 3;
                }
            }

            if (lerpNorth == 1)
            {
                for (int x = 0; x < resolution - 2; x++)
                {
                    int vertexIndex = x + resolution * (resolution - 2);
                    if (x % 2 == 0)
                    {
                        triangles[triangleIndex] = vertexIndex + resolution;
                        triangles[triangleIndex + 1] = vertexIndex + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution + 2;
                        triangleIndex += 3;
                    }
                    else
                    {
                        triangles[triangleIndex] = vertexIndex;
                        triangles[triangleIndex + 1] = vertexIndex + 1;
                        triangles[triangleIndex + 2] = vertexIndex + resolution + 1;
                        triangles[triangleIndex + 3] = vertexIndex + 1;
                        triangles[triangleIndex + 4] = vertexIndex + 2;
                        triangles[triangleIndex + 5] = vertexIndex + resolution + 1;
                        triangleIndex += 6;
                    }
                }
                if (lerpWest == 0)
                {
                    int vertexIndex = resolution * (resolution - 2);
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution;
                    triangleIndex += 3;
                }
                if (lerpEast == 0)
                {
                    int vertexIndex = resolution * (resolution - 2) + resolution - 2;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = vertexIndex + 1;
                    triangles[triangleIndex + 2] = vertexIndex + resolution + 1;
                    triangleIndex += 3;
                }
            }

            return triangles;
        }
    }
}
