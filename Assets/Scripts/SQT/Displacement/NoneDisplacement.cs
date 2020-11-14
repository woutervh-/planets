using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SQT.Displacement
{
    public class NoneDisplacement : MeshModifier
    {
        public async Task ModifyVertices(SQT.Context context, SQT.Node node, CancellationTokenSource cancellation)
        {
        }

        public void ModifyMaterial(Material material)
        {
        }

        public void Destroy()
        {
        }
    }
}
