using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SQT
{
    public static class Reconciler
    {
        public static void Initialize(Context context)
        {
            HashSet<Node> marked = new HashSet<Node>();
            MarkRoots(context, marked);
            PerformMarkAndSweep(context, marked);
        }

        public static void Reconcile(Context context, ReconciliationData reconciliationData)
        {
            HashSet<Node> marked = new HashSet<Node>();
            MarkRoots(context, marked);

            // Perform deep split.
            Node root = context.roots[reconciliationData.branch.index];
            Node leaf = DeepSplit(context, reconciliationData, root);
            marked.Add(leaf);

            // Mark the eight surrounding quads of the leaf.
            MarkEightNeighbors(context, marked, leaf);

            // Mark nodes to create a balanced tree (max 2:1 split between neighbors).
            MarkBalancedNodes(context, marked);

            PerformMarkAndSweep(context, marked);
        }

        static void PerformMarkAndSweep(Context context, HashSet<Node> marked)
        {
            // Ensure parents and siblings of marked nodes are marked as well.
            MarkRequiredNodes(marked, context.roots);

            // Walk quad tree and sweep unmarked nodes.
            Sweep(marked, context.roots);

            // Request meshes.
            MakeMeshRequests(context, context.roots);

            // Set mesh visibilities.
            DetermineVisibleMeshes(context, context.roots);
        }

        static void DetermineVisibleMeshes(Context context, Node[] nodes)
        {
            foreach (Node node in nodes)
            {
                if (node.mesh == null)
                {
                    continue;
                }

                int neighborMask = 0;
                for (int i = 0; i < 4; i++)
                {
                    Node neighbor = GetNeighbor(context, node, i);
                    if (neighbor == null || neighbor.mesh == null || (neighbor.parent != null && !AreChildMeshesLoaded(neighbor.parent)))
                    {
                        neighborMask |= 1 << i;
                    }
                }

                if (node.children != null && neighborMask == 0 && AreChildMeshesLoaded(node))
                {
                    node.meshRenderer.enabled = false;
                    DetermineVisibleMeshes(context, node.children);
                }
                else
                {
                    if (node.mesh.triangles != context.triangles[neighborMask].triangles)
                    {
                        node.mesh.triangles = context.triangles[neighborMask].triangles;
                        node.mesh.RecalculateBounds();
                    }
                    node.meshRenderer.enabled = true;
                }
            }
        }

        static bool AreChildMeshesLoaded(Node parent)
        {
            foreach (Node child in parent.children)
            {
                if (child.mesh == null)
                {
                    return false;
                }
            }
            return true;
        }

        static void MakeMeshRequests(Context context, Node[] nodes)
        {
            foreach (Node node in nodes)
            {
                if (node.meshRequest == null)
                {
                    node.meshRequestCancellation = new CancellationTokenSource();
                    node.meshRequest = node.RequestMesh(context);
                }
                if (node.children != null)
                {
                    MakeMeshRequests(context, node.children);
                }
            }
        }

        static void Sweep(HashSet<Node> marked, Node[] nodes)
        {
            foreach (Node node in nodes)
            {
                if (node.children != null)
                {
                    Sweep(marked, node.children);
                    if (!HasMarkedChild(marked, node))
                    {
                        Node.RemoveChildren(node);
                    }
                }
                if (!marked.Contains(node))
                {
                    node.Destroy();
                }
            }
        }

        static void MarkRoots(Context context, HashSet<Node> marked)
        {
            foreach (Node root in context.roots)
            {
                marked.Add(root);
            }
        }

        static void MarkEightNeighbors(Context context, HashSet<Node> marked, Node leaf)
        {
            Node n1 = EnsureNeighbor(context, leaf, 0);
            Node n2 = EnsureNeighbor(context, leaf, 1);
            Node n3 = EnsureNeighbor(context, leaf, 2);
            Node n4 = EnsureNeighbor(context, leaf, 3);
            Node n5 = EnsureNeighbor(context, n1, 3);
            Node n6 = EnsureNeighbor(context, n2, 2);
            Node n7 = EnsureNeighbor(context, n3, 0);
            Node n8 = EnsureNeighbor(context, n4, 1);
            marked.Add(n1);
            marked.Add(n2);
            marked.Add(n3);
            marked.Add(n4);
            marked.Add(n5);
            marked.Add(n6);
            marked.Add(n7);
            marked.Add(n8);
        }

        static void MarkBalancedNodes(Context context, HashSet<Node> marked)
        {
            HashSet<Node> seen = new HashSet<Node>();
            Stack<Node> remaining = new Stack<Node>();
            foreach (Node node in marked)
            {
                remaining.Push(node);
                seen.Add(node);
            }
            while (remaining.Count >= 1)
            {
                Node current = remaining.Pop();

                if (current.path.Length <= 1)
                {
                    continue;
                }

                Node[] parentNeighbors = new Node[] {
                    EnsureNeighbor(context, current.parent, 0),
                    EnsureNeighbor(context, current.parent, 1),
                    EnsureNeighbor(context, current.parent, 2),
                    EnsureNeighbor(context, current.parent, 3)
                };

                for (int i = 0; i < 4; i++)
                {
                    if (!seen.Contains(parentNeighbors[i]))
                    {
                        seen.Add(parentNeighbors[i]);
                        remaining.Push(parentNeighbors[i]);
                    }
                }
            }
            marked.UnionWith(seen);
        }

        static void MarkRequiredNodes(HashSet<Node> marked, Node[] nodes)
        {
            foreach (Node node in nodes)
            {
                if (node.children != null)
                {
                    MarkRequiredNodes(marked, node.children);
                    if (HasMarkedChild(marked, node))
                    {
                        marked.Add(node);
                        foreach (Node child in node.children)
                        {
                            marked.Add(child);
                        }
                    }
                }
            }
        }

        static bool HasMarkedChild(HashSet<Node> marked, Node node)
        {
            foreach (Node child in node.children)
            {
                if (marked.Contains(child))
                {
                    return true;
                }
            }
            return false;
        }

        static Node EnsureChild(Context context, Node node, int ordinal)
        {
            if (node.children == null)
            {
                Node.CreateChildren(context, node);
            }
            return node.children[ordinal];
        }

        static Node GetChild(Node node, int ordinal)
        {
            if (node.children == null)
            {
                return null;
            }
            return node.children[ordinal];
        }

        static Node EnsureRelativePath(Context context, Node node, int[] path)
        {
            Node current = node;
            for (int i = 0; i < path.Length; i++)
            {
                current = EnsureChild(context, current, path[i]);
            }
            return current;
        }

        static Node GetRelativePath(Node node, int[] path)
        {
            Node current = node;
            for (int i = 0; i < path.Length && current != null; i++)
            {
                current = GetChild(current, path[i]);
            }
            return current;
        }

        static Node GetNeighbor(Context context, Node node, int direction)
        {
            return GetRelativePath(context.roots[node.neighborBranches[direction]], node.neighborPaths[direction]);
        }

        public static Node EnsureNeighbor(Context context, Node node, int direction)
        {
            return EnsureRelativePath(context, context.roots[node.neighborBranches[direction]], node.neighborPaths[direction]);
        }

        static int GetChildOrdinal(Vector2 pointInPlane, Vector2 offset, float scale)
        {
            Vector2 t = (pointInPlane - offset) / scale;
            return (t.x < 0 ? 0 : 1) + (t.y < 0 ? 0 : 2);
        }

        static bool ShouldSplit(Context context, ReconciliationData reconciliationData, Node node)
        {
            return node.path.Length < context.constants.maxDepth
                && node.depth.approximateSize > reconciliationData.desiredLength;
        }

        static Node DeepSplit(Context context, ReconciliationData reconciliationData, Node node)
        {
            if (ShouldSplit(context, reconciliationData, node))
            {
                int ordinal = GetChildOrdinal(reconciliationData.pointInPlane, node.offset, node.depth.scale);
                return DeepSplit(context, reconciliationData, EnsureChild(context, node, ordinal));
            }
            else
            {
                return node;
            }
        }
    }
}
