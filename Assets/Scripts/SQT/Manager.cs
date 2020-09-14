using System;
using UnityEngine;

namespace SQT
{
    public class Manager : MonoBehaviour
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

        public int seed = 0;
        public float strength = 0.1f;
        public float frequency = 1f;
        public float lacunarity = 2f;
        public float persistence = 0.5f;
        public int octaves = 8;

        bool dirty;
        Camera playerCamera;
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

        void Update()
        {
            if (dirty)
            {
                dirty = false;
                DoCleanup();
                DoUpdate();
            }

            if (playerCamera == null || playerCamera.gameObject != player)
            {
                playerCamera = player.GetComponent<Camera>();
            }

            ReconciliationData reconciliationData = ReconciliationData.GetData(context, playerCamera);
            if (reconciliationData == null)
            {
                return;
            }

            Reconciler.Reconcile(context, reconciliationData);
        }

        void DoUpdate()
        {
            MeshModifier verticesModifier = new PerlinDisplacementCPU(seed)
            {
                strength = strength,
                frequency = frequency,
                lacunarity = lacunarity,
                persistence = persistence,
                octaves = octaves
            };

            verticesModifier.ModifyMaterial(material);

            Context.Constants constants = new Context.Constants
            {
                desiredScreenSpaceLength = desiredScreenSpaceLength,
                gameObject = gameObject,
                material = material,
                maxDepth = maxDepth,
                resolution = resolution * 2 - 1, // We can only use odd resolutions.,
                verticesModifier = verticesModifier
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
