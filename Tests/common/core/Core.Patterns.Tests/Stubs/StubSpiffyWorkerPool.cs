using Common.Core.Patterns.Pipelines.Spiffy;
using System.Threading.Tasks;

namespace Common.Core.Patterns.Tests.Stubs
{
    internal class StubSpiffyWorkerPool<T, TMessage> : ISpiffyWorkerPool<T, TMessage>, ISpiffyContext<T>
        where T : new()
    {
        public virtual SpiffyPoolOptions PoolOptions { get; } = null;

        public virtual int ActiveWorkers { get; } = 0;

        public virtual int IdleWorkers { get; } = 0;

        public StubSpiffyWorkerPool()
        { }

        public StubSpiffyWorkerPool(SpiffyPoolOptions poolOptions = default)
        { }

        public virtual void CancelAll()
        {
            
        }

        public virtual void Dispose()
        {
            
        }

        ValueTask ISpiffyWorkerPool<T, TMessage>.Post(TMessage item)
            => new ValueTask();

        ValueTask ISpiffyContext<T>.Reclaim(ISpiffyWorker<T> spiffyWorker)
            => new ValueTask();
    }
}
