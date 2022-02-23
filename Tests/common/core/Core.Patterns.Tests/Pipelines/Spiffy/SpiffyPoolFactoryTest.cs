using Common.Core.Patterns.Pipelines.Spiffy;
using Common.Core.Patterns.Pipelines.Spiffy.Pools;
using Xunit;

namespace Common.Core.Patterns.Tests.Pipelines.Spiffy
{
    public class SpiffyPoolFactoryTest
    {
        [Fact]
        public void CreatePool_ReturnsAnInstanceOfASpiffyWorker_When_Called()
        {
            ISpiffyWorkerPool<int, int> workerPool = SpiffyWorkerPoolFactory.CreatePool<int, int>();

            Assert.NotNull(workerPool);

            Assert.IsType<SpiffyWorkerPool<int, int>>(workerPool);
        }
    }
}
