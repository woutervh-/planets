using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SQT
{
    public interface MeshModifier
    {
        Task ModifyVertices(Context context, Node node, CancellationTokenSource cancellation);
        void ModifyMaterial(Material material);
        void Destroy();
    }
}
