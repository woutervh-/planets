using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace SQT.Displacement
{
    public class PerlinDisplacementGPU : MeshModifier
    {
        public float strength;
        public float frequency;
        public float lacunarity;
        public float persistence;
        public int octaves;

        private ComputeShader computeShader;
        private Noise.Perlin perlin;
        private Texture2D gradientsTexture;
        private Texture2D permutationTexture;
        private ComputeBuffer positionBuffer;
        private ComputeBuffer normalBuffer;
        private int computeKernel;

        public PerlinDisplacementGPU(int seed, ComputeShader computeShader)
        {
            this.computeShader = computeShader;
            perlin = new Noise.Perlin(seed);
            gradientsTexture = Noise.PerlinTextureGenerator.CreateGradientsTexture(perlin);
            permutationTexture = Noise.PerlinTextureGenerator.CreatePermutationTexture(perlin);
            computeKernel = computeShader.FindKernel("GenerateSurface");
        }

        public async Task ModifyVertices(Context context, Node node, CancellationTokenSource cancellation)
        {
            if (positionBuffer == null || positionBuffer.count != node.positions.Length)
            {
                if (positionBuffer != null)
                {
                    positionBuffer.Release();
                }
                positionBuffer = new ComputeBuffer(node.positions.Length, 3 * 4);
            }
            if (normalBuffer == null || normalBuffer.count != node.normals.Length)
            {
                if (normalBuffer != null)
                {
                    normalBuffer.Release();
                }
                normalBuffer = new ComputeBuffer(node.normals.Length, 3 * 4);
            }

            positionBuffer.SetData(node.positions);
            normalBuffer.SetData(node.normals);

            computeShader.SetBuffer(computeKernel, "positionBuffer", positionBuffer);
            computeShader.SetBuffer(computeKernel, "normalBuffer", normalBuffer);
            computeShader.SetTexture(computeKernel, "_Gradients2D", gradientsTexture);
            computeShader.SetTexture(computeKernel, "_Permutation2D", permutationTexture);
            computeShader.SetFloat("_Strength", strength);
            computeShader.SetFloat("_Frequency", frequency);
            computeShader.SetFloat("_Lacunarity", lacunarity);
            computeShader.SetFloat("_Persistence", persistence);
            computeShader.SetInt("_Octaves", octaves);

            uint x, y, z;
            computeShader.GetKernelThreadGroupSizes(computeKernel, out x, out y, out z);
            float total = x * y * z;
            computeShader.Dispatch(computeKernel, Mathf.CeilToInt(node.positions.Length / total), 1, 1);

            Task positionsTask = new Task(() => { });
            Task normalsTask = new Task(() => { });
            Action<AsyncGPUReadbackRequest> positionsAction = new Action<AsyncGPUReadbackRequest>((request) =>
            {
                node.positions = request.GetData<Vector3>().ToArray();
                positionsTask.Start();
            });
            Action<AsyncGPUReadbackRequest> normalsAction = new Action<AsyncGPUReadbackRequest>((request) =>
            {
                node.normals = request.GetData<Vector3>().ToArray();
                normalsTask.Start();
            });

            AsyncGPUReadback.Request(positionBuffer, positionsAction);
            AsyncGPUReadback.Request(normalBuffer, normalsAction);

            await positionsTask;
            await normalsTask;
        }

        public void ModifyMaterial(Material material)
        {
            material.SetTexture("_Gradients2D", gradientsTexture);
            material.SetTexture("_Permutation2D", permutationTexture);
            material.SetFloat("_Strength", strength);
            material.SetFloat("_Frequency", frequency);
            material.SetFloat("_Lacunarity", lacunarity);
            material.SetFloat("_Persistence", persistence);
            material.SetInt("_Octaves", octaves);
        }

        public void Destroy()
        {
            if (positionBuffer != null)
            {
                positionBuffer.Release();
                positionBuffer = null;
            }
            if (normalBuffer != null)
            {
                normalBuffer.Release();
                normalBuffer = null;
            }
            UnityEngine.Object.Destroy(gradientsTexture);
            UnityEngine.Object.Destroy(permutationTexture);
        }
    }
}
