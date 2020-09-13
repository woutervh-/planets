using System.Threading;
using System.Threading.Tasks;

namespace SQT
{
    public interface VerticesModifier
    {
        Task ModifyVertices(Context context, Node node, CancellationTokenSource cancellation);
    }
}
