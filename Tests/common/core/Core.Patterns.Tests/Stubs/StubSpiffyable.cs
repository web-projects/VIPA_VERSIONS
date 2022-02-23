using Common.Core.Patterns.Pipelines.Spiffy;
using System;
using System.Threading.Tasks;

namespace Common.Core.Patterns.Tests.Stubs
{
    internal class StubSpiffyable : ISpiffyable<int>, IDisposable
    {
        public virtual void Dispose()
        {
            
        }

        public virtual Task Act(ISpiffyNotifiable notifier, int context)
        {
            notifier.AllSpiffy();
            return Task.CompletedTask;
        }
    }
}
