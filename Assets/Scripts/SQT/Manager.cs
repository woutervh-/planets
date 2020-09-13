using System;
using UnityEngine;

namespace SQT
{
    public class SQTManager : MonoBehaviour
    {
        public GameObject player;
        [Range(0f, 16f)]
        public int maxDepth = 10;
        [Range(2f, 16f)]
        public int resolution = 7;
        [Range(1f, 100f)]
        public float desiredScreenSpaceLength = 10f;
        public Material material;

#if UNITY_EDITOR
        public bool debug;
#endif

        bool dirty;
        Context context;

        void OnEnable()
        {
            dirty = false;
            DoUpdate();
        }

        void OnDisable()
        {
            DoCleanup();
        }

        void OnValidate()
        {
            dirty = true;
        }

        void HandleChange(object sender, EventArgs e)
        {
            dirty = true;
        }

        void DoUpdate()
        {
            Context.Constants constants = new Context.Constants
            {
                desiredScreenSpaceLength = desiredScreenSpaceLength,
                gameObject = gameObject,
                material = material,
                maxDepth = maxDepth,
                resolution = resolution * 2 - 1, // We can only use odd resolutions.,
                meshDisplacement = new PerlinDisplacementCPU()
            };

            Context.Branch[] branches = Context.Branch.GetFromConstants(constants);
            Context.Depth[] depths = Context.Depth.GetFromConstants(constants);
            Context.Triangles[] triangles = Context.Triangles.GetFromConstants(constants);
            Node[] roots = new Node[6];
            for (int i = 0; i < 6; i++)
            {
                roots[i] = Node.CreateRoot(constants, depths[0], branches[i]);
            }

            context = new Context
            {
                constants = constants,
                branches = branches,
                depths = depths,
                triangles = triangles,
                roots = roots
            };

            Reconciler.Initialize(context);
        }

        void DoCleanup()
        {
            for (int i = 0; i < 6; i++)
            {
                UnityEngine.Object.Destroy(context.branches[i].gameObject);
                context.roots[i].Destroy();
            }
        }
    }
}
