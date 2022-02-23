using System.Threading.Tasks;

namespace Common.Core.Patterns.Pipelines.Spiffy
{
    internal interface ISpiffyContext<T> where T : new()
    {
        ValueTask Reclaim(ISpiffyWorker<T> spiffyWorker);
    }
}
