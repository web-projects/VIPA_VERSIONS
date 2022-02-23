using Common.Core.Patterns.Pipelines.Spiffy;
using Common.Core.Patterns.Pipelines.Spiffy.Pools;
using Common.Core.Patterns.Tests.Stubs;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace Common.Core.Patterns.Tests.Pipelines.Spiffy.Pools
{
    public class SpiffyWorkerPoolTest : IDisposable
    {
        readonly SpiffyWorkerPool<Mock<StubSpiffyable>, int> subject;

        public SpiffyWorkerPoolTest()
            => subject = new SpiffyWorkerPool<Mock<StubSpiffyable>, int>();

        public void Dispose() => subject.Dispose();

        [Fact]
        public void Ctor_ShouldAssignDefaultPoolOptions_When_NoneIsSpecified()
            => Assert.Equal(SpiffyPoolOptions.Default, subject.PoolOptions);

        [Fact]
        public async void Post_ShouldThrowObjectDisposedException_When_ObjectIsDisposed()
        {
            TestHelper.Helper.SetFieldValueToInstance<bool>("disposed", false, false, subject, true);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => subject.Post(0).AsTask());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async void Post_ShoudPostMessageAccordingTo_PipelineAvailability_When_Called(bool availability)
        {
            Mock<StubSpiffyable> mockSpiffyable = new Mock<StubSpiffyable>();

            using SpiffyWorkerPool<StubSpiffyable, int> workerPool = new SpiffyWorkerPool<StubSpiffyable, int>(
                () =>
                {
                    return mockSpiffyable.Object;
                });

            using ManualResetEvent resetEvent = new ManualResetEvent(false);

            mockSpiffyable.Setup(e => e.Act(It.IsAny<ISpiffyNotifiable>(), It.IsAny<int>()))
                .Callback(() => resetEvent.Set());

            while (!TestHelper.Helper.GetFieldValueFromInstance<bool>("pipelineActive", false, false, workerPool))
            {
                await Task.Delay(50);
            }

            TestHelper.Helper.SetFieldValueToInstance<bool>("pipelineActive", false, false, workerPool, availability);

            await workerPool.Post(0);

            if (availability)
            {
                resetEvent.WaitOne(2000);
            }

            mockSpiffyable.Verify(e => e.Act(It.IsAny<ISpiffyNotifiable>(), It.IsAny<int>()), Times.Exactly(availability ? 1 : 0));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async void Reclaim_ShouldReclaimAndDisposeOfShortLivedWorkers_IfNecessary_When_Called(bool shortLived)
        {
            Mock<StubSpiffyable> mockSpiffyable = new Mock<StubSpiffyable>();

            using SpiffyWorkerPool<StubSpiffyable, int> workerPool = new SpiffyWorkerPool<StubSpiffyable, int>(
                new SpiffyPoolOptions(10),
                () =>
                {
                    return mockSpiffyable.Object;
                });

            ConcurrentQueue<ISpiffyWorker<StubSpiffyable>> idleWorkerQueue
                = TestHelper.Helper.GetFieldValueFromInstance<ConcurrentQueue<ISpiffyWorker<StubSpiffyable>>>(
                    "idleWorkerQueue", false, false, workerPool);

            ConcurrentDictionary<int, ISpiffyWorker<StubSpiffyable>> busyWorkerMap
                = TestHelper.Helper.GetFieldValueFromInstance<ConcurrentDictionary<int, ISpiffyWorker<StubSpiffyable>>>(
                    "busyWorkerMap", false, false, workerPool);

            Mock<ISpiffyWorker<StubSpiffyable>> mockSpiffyWorker = new Mock<ISpiffyWorker<StubSpiffyable>>();
            mockSpiffyWorker.SetupGet(e => e.SpiffyableObject).Returns(mockSpiffyable.Object);
            mockSpiffyWorker.SetupGet(e => e.Lifetime).Returns(shortLived ? SpiffyLifetime.Short : SpiffyLifetime.Long);
            mockSpiffyWorker.SetupGet(e => e.WorkerId).Returns(12);

            idleWorkerQueue.Clear();
            busyWorkerMap.TryAdd(12, mockSpiffyWorker.Object);

            await workerPool.Reclaim(mockSpiffyWorker.Object);

            if (shortLived)
            {
                mockSpiffyWorker.Verify(e => e.Dispose(), Times.Once());
            }
            else
            {
                Assert.Single(idleWorkerQueue);
            }
        }

        [Fact]
        public async void CancelAll_ShouldCompleteTheNetworkBlock_And_CancelAllWork_WhenCalled()
        {
            subject.CancelAll();

            CancellationTokenSource tokenSource =
                TestHelper.Helper.GetFieldValueFromInstance<CancellationTokenSource>("cancellationTokenSource", false, false, subject);

            BufferBlock<int> bufferBlock
                = TestHelper.Helper.GetFieldValueFromInstance<BufferBlock<int>>("headNetworkBlock", false, false, subject);

            await Task.Delay(128);

            Assert.True(tokenSource.IsCancellationRequested);
            Assert.True(bufferBlock.Completion.IsCompleted);
        }

        [Fact]
        public async void Dispose_ShouldCancelAllWork_And_ClearIdleWorkers_When_Called()
        {
            CancellationTokenSource tokenSource =
              TestHelper.Helper.GetFieldValueFromInstance<CancellationTokenSource>("cancellationTokenSource", false, false, subject);

            BufferBlock<int> bufferBlock
                = TestHelper.Helper.GetFieldValueFromInstance<BufferBlock<int>>("headNetworkBlock", false, false, subject);

            ConcurrentQueue<ISpiffyWorker<Mock<StubSpiffyable>>> idleWorkerQueue
               = TestHelper.Helper.GetFieldValueFromInstance<ConcurrentQueue<ISpiffyWorker<Mock<StubSpiffyable>>>>(
                   "idleWorkerQueue", false, false, subject);

            subject.Dispose();

            await Task.Delay(128);

            Assert.True(tokenSource.IsCancellationRequested);
            Assert.True(bufferBlock.Completion.IsCompleted);
            Assert.Empty(idleWorkerQueue);
        }
    }
}
