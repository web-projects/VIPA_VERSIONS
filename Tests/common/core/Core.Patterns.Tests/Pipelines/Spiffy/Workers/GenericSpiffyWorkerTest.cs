using Common.Core.Patterns.Pipelines.Spiffy;
using Common.Core.Patterns.Pipelines.Spiffy.Workers;
using Common.Core.Patterns.Tests.Stubs;
using Moq;
using System;
using System.Threading;
using Xunit;

namespace Common.Core.Patterns.Tests.Pipelines.Spiffy.Workers
{
    public class GenericSpiffyWorkerTest
    {
        readonly GenericSpiffyWorker<StubSpiffyable> subject;
        readonly Mock<StubSpiffyable> mockSpiffyable;
        readonly Mock<StubSpiffyWorkerPool<StubSpiffyable, int>> mockSpiffyWorkerPool;

        public GenericSpiffyWorkerTest()
        {
            mockSpiffyable = new Mock<StubSpiffyable>();
            mockSpiffyWorkerPool = new Mock<StubSpiffyWorkerPool<StubSpiffyable, int>>();

            subject = new GenericSpiffyWorker<StubSpiffyable>(ValidActivator, mockSpiffyWorkerPool.Object, SpiffyLifetime.Long);
        }

        private Func<StubSpiffyable> ValidActivator => () => mockSpiffyable.Object;
        private Func<StubSpiffyable> InvalidActivator => () => null;

        [Fact]
        public void Ctor_ShouldThrowArgumentNullException_When_ActivatorIsNull()
            => Assert.Throws<ArgumentNullException>(
                () => new GenericSpiffyWorker<StubSpiffyable>(null, mockSpiffyWorkerPool.Object, SpiffyLifetime.Long));

        [Fact]
        public void Ctor_ShouldThrowArgumentNullException_When_SpiffyContextIsNull()
            => Assert.Throws<ArgumentNullException>(
                () => new GenericSpiffyWorker<StubSpiffyable>(InvalidActivator, null, SpiffyLifetime.Long));

        [Fact]
        public void Ctor_ShouldThrowNullReferenceException_When_ActivatorIsInvalid_And_ReturnsNull()
            => Assert.Throws<NullReferenceException>(
                () => new GenericSpiffyWorker<StubSpiffyable>(InvalidActivator, mockSpiffyWorkerPool.Object, SpiffyLifetime.Long));

        [Fact]
        public void Ctor_ShouldSuccessfullyInitialize_When_ActivatorAndContextAreValid()
        {
            StubSpiffyable spiffyableObject = TestHelper.Helper.GetPropertyValueFromInstance<StubSpiffyable>(
                "SpiffyableObject", true, false, subject);

            ISpiffyContext<StubSpiffyable> spiffyContext = TestHelper.Helper.GetPropertyValueFromInstance<ISpiffyContext<StubSpiffyable>>(
                "SpiffyContext", false, false, subject);

            Assert.Same(mockSpiffyable.Object, spiffyableObject);
            Assert.Same(mockSpiffyWorkerPool.Object, spiffyContext);
        }

        [Fact]
        public void Dispose_ShouldCallDisposeOnSpiffyableObject_When_ItIsDisposable()
        {
            subject.Dispose();

            mockSpiffyable.Verify(e => e.Dispose(), Times.Once());
        }

        [Fact]
        public async void RunAsync_ShouldPutWorkerInBusyState_When_Called()
        {
            await subject.RunAsync<int>();

            Assert.Equal(SpiffyState.Busy, subject.State);
        }

        [Fact]
        public void RunAsync_ShouldThrowNullReferenceException_When_SpiffyableObjectIsntConvertible()
            => Assert.ThrowsAsync<NullReferenceException>(() => new GenericSpiffyWorker<int>(
                () => 5,
                new Mock<ISpiffyContext<int>>().Object, 
                SpiffyLifetime.Long
            ).RunAsync<int>().AsTask());

        [Fact]
        public async void AllSpiffy_ShouldResetTheWorkerToIdle_When_Called()
        {
            using ManualResetEvent resetEvent = new ManualResetEvent(false);

            mockSpiffyable.Setup(e => e.Act(It.IsAny<ISpiffyNotifiable>(), It.IsAny<int>()))
                .Callback(() => resetEvent.Set());

            await subject.RunAsync<int>();

            Assert.True(resetEvent.WaitOne(2000));

            subject.AllSpiffy();

            Assert.Equal(SpiffyState.Idle, subject.State);
        }
    }
}
