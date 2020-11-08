using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SQT
{
    public class Node
    {
        public Node parent;
        public Node[] children;
        public int[] path;
        public int[] neighborBranches;
        public int[][] neighborPaths;
        public Context.Branch branch;
        public Context.Depth depth;
        public Vector2 offset;
        public GameObject gameObject;
        public Vector3[] positions;
        public Vector3[] normals;
        public int[] triangles;
        public Mesh mesh;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public Task meshRequest;
        public CancellationTokenSource meshRequestCancellation;

        public static Node CreateRoot(Context.Constants constants, Context.Depth depth, Context.Branch branch)
        {
            GameObject gameObject = new GameObject("Chunk");
            gameObject.transform.SetParent(branch.gameObject.transform, false);
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.enabled = false;
            meshRenderer.sharedMaterial = constants.material;

            int[] path = new int[0];
            int[] neighborBranches;
            int[][] neighborPaths;
            GetNeighborPaths(path, branch.index, out neighborBranches, out neighborPaths);

            return new Node
            {
                parent = null,
                children = null,
                path = path,
                neighborBranches = neighborBranches,
                neighborPaths = neighborPaths,
                branch = branch,
                depth = depth,
                offset = Vector2.zero,
                gameObject = gameObject,
                mesh = null,
                meshFilter = meshFilter,
                meshRenderer = meshRenderer,
                meshRequest = null,
                meshRequestCancellation = null
            };
        }

        public static Node CreateChild(Context context, Node parent, int ordinal)
        {
            int[] path = GetChildPath(parent.path, ordinal);
            int[] neighborBranches;
            int[][] neighborPaths;
            GetNeighborPaths(path, parent.branch.index, out neighborBranches, out neighborPaths);

            GameObject gameObject = new GameObject("Chunk " + string.Join("", path));
            gameObject.transform.SetParent(parent.branch.gameObject.transform, false);
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.enabled = false;
            meshRenderer.sharedMaterial = context.constants.material;
            Context.Depth depth = context.depths[parent.depth.index + 1];

            return new Node
            {
                parent = parent,
                children = null,
                path = path,
                neighborBranches = neighborBranches,
                neighborPaths = neighborPaths,
                branch = parent.branch,
                depth = depth,
                offset = parent.offset + childOffsetVectors[ordinal] * depth.scale,
                gameObject = gameObject,
                mesh = null,
                meshFilter = meshFilter,
                meshRenderer = meshRenderer,
                meshRequest = null,
                meshRequestCancellation = null
            };
        }

        public static void CreateChildren(Context context, Node parent)
        {
            parent.children = new Node[4];
            for (int i = 0; i < 4; i++)
            {
                parent.children[i] = CreateChild(context, parent, i);
            }
        }

        public static void RemoveChildren(Node parent)
        {
            for (int i = 0; i < 4; i++)
            {
                parent.children[i].Destroy();
            }
            parent.children = null;
        }

        static int[] GetChildPath(int[] path, int ordinal)
        {
            int[] childPath = new int[path.Length + 1];
            Array.Copy(path, childPath, path.Length);
            childPath[path.Length] = ordinal;
            return childPath;
        }

        static void GetNeighborPaths(int[] path, int branch, out int[] neighborBranches, out int[][] neighborPaths)
        {
            neighborBranches = new int[4];
            neighborPaths = new int[4][];
            for (int i = 0; i < 4; i++)
            {
                int neighborBranch;
                int[] neighborPath;
                GetNeighborPath(path, branch, i, out neighborBranch, out neighborPath);
                neighborBranches[i] = neighborBranch;
                neighborPaths[i] = neighborPath;
            }
        }

        static void GetNeighborPath(int[] path, int branch, int direction, out int neighborBranch, out int[] neighborPath)
        {
            int commonAncestorDistance = 1;
            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (Node.neighborSameParent[path[i]][direction])
                {
                    break;
                }
                commonAncestorDistance += 1;
            }

            neighborPath = new int[path.Length];
            if (commonAncestorDistance <= path.Length)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    if (i < commonAncestorDistance)
                    {
                        neighborPath[path.Length - i - 1] = Node.neighborOrdinal[path[path.Length - i - 1]][direction];
                    }
                    else
                    {
                        neighborPath[path.Length - i - 1] = path[path.Length - i - 1];
                    }
                }
            }
            else
            {
                int fromOrdinal = branch;
                int toOrdinal = Node.rootOrdinalRotation[fromOrdinal][direction];
                for (int i = 0; i < path.Length; i++)
                {
                    neighborPath[i] = Node.neighborOrdinalRotation[fromOrdinal][toOrdinal][path[i]];
                }
            }

            if (commonAncestorDistance <= path.Length)
            {
                neighborBranch = branch;
            }
            else
            {
                neighborBranch = Node.rootOrdinalRotation[branch][direction];
            }
        }

        public async Task RequestMesh(Context context)
        {
            MeshHelper.GenerateVertices(context, this, out positions, out normals);

            await context.constants.verticesModifier.ModifyVertices(context, this, meshRequestCancellation);

            if (!meshRequestCancellation.Token.IsCancellationRequested)
            {
                mesh = new Mesh();
                mesh.vertices = positions;
                mesh.normals = normals;
                meshFilter.sharedMesh = mesh;
            }
        }

        public void Destroy()
        {
            if (meshRequestCancellation != null)
            {
                meshRequestCancellation.Cancel();
            }
            if (mesh != null)
            {
                UnityEngine.Object.Destroy(mesh);
            }
            if (meshFilter != null)
            {
                UnityEngine.Object.Destroy(meshFilter);
            }
            if (meshRenderer != null)
            {
                UnityEngine.Object.Destroy(meshRenderer);
            }
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }

        public static Vector2[] childOffsetVectors = new Vector2[] {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(-1f, 1f),
            new Vector2(1f, 1f),
        };

        public static bool[][] neighborSameParent = new bool[][] {
            new bool[] { false, true, false, true },
            new bool[] { true, false, false, true },
            new bool[] { false, true, true, false },
            new bool[] { true, false, true, false }
        };

        public static int[][] neighborOrdinal = new int[][] {
            new int[] { 1, 1, 2, 2 },
            new int[] { 0, 0, 3, 3 },
            new int[] { 3, 3, 0, 0 },
            new int[] { 2, 2, 1, 1 }
        };

        public static int[][] rootOrdinalRotation = new int[][] {
            new int[] { 2, 3, 4, 5 },
            new int[] { 3, 2, 4, 5 },
            new int[] { 4, 5, 0, 1 },
            new int[] { 5, 4, 0, 1 },
            new int[] { 1, 0, 3, 2 },
            new int[] { 0, 1, 3, 2 }
        };

        public static int[][][] neighborOrdinalRotation = new int[][][] {
            new int[][] {
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 3, 1, 2, 0 }
            },
            new int[][] {
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 0, 2, 1, 3 }
            },
            new int[][] {
                new int[] { 0, 2, 1, 3 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 0, 2, 1, 3 }
            },
            new int[][] {
                new int[] { 3, 1, 2, 0 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 0, 2, 1, 3 }
            },
            new int[][] {
                new int[] { 3, 1, 2, 0 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 3, 1, 2, 0 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 }
            },
            new int[][] {
                new int[] { 3, 1, 2, 0 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 0, 2, 1, 3 },
                new int[] { 0, 1, 2, 3 },
                new int[] { 0, 1, 2, 3 }
            }
        };
    }
}
