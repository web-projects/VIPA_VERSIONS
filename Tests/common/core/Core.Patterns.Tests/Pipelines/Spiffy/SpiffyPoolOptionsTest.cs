using Common.Core.Patterns.Pipelines.Spiffy;
using Xunit;

namespace Common.Core.Patterns.Tests.Pipelines.Spiffy
{
    public class SpiffyPoolOptionsTest
    {
        readonly SpiffyPoolOptions subject;

        public SpiffyPoolOptionsTest() 
            => subject = new SpiffyPoolOptions();

        [Fact]
        public void DefaultBufferThreshold_ShouldBeAsExpected_When_Called()
            => Assert.Equal(-1, SpiffyPoolOptions.DefaultBufferThreshold);

        [Fact]
        public void DefaultWorkerCount_ShouldBeAsExpected_When_Called()
            => Assert.Equal(20, SpiffyPoolOptions.DefaultWorkerCount);

        [Fact]
        public void DefaultSpiffyOptions_ShouldStayTheSame_When_CalledAndHave_DefaultValues()
        {
            SpiffyPoolOptions optionsA = SpiffyPoolOptions.Default;
            SpiffyPoolOptions optionsB = SpiffyPoolOptions.Default;

            Assert.Same(optionsA, optionsB);
            Assert.Equal(SpiffyPoolOptions.DefaultBufferThreshold, optionsA.MaxBufferThreshold);
            Assert.Equal(SpiffyPoolOptions.DefaultWorkerCount, optionsA.NumberOfWorkers);
        }

        [Fact]
        public void NumberOfWorkers_ShouldBeBoundToDefaultValue_When_CalledWithNoOptions()
            => Assert.Equal(SpiffyPoolOptions.DefaultWorkerCount, subject.NumberOfWorkers);

        [Fact]
        public void MaxBufferThreshold_ShouldBeBoundToDefaultValue_When_CalledWithNoOptions()
            => Assert.Equal(SpiffyPoolOptions.DefaultBufferThreshold, subject.MaxBufferThreshold);

        [Fact]
        public void Ctor_ShouldSetNumberOfWorkers_When_Provided()
            => Assert.Equal(5, new SpiffyPoolOptions(5).NumberOfWorkers);

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Ctor_ShouldSetNumberOfWorkersToDefault_When_NumberIsZeroOrLess(int value)
            => Assert.Equal(SpiffyPoolOptions.DefaultWorkerCount, new SpiffyPoolOptions(value).NumberOfWorkers);

        [Fact]
        public void Ctor_ShouldSetMaxBufferCount_When_Provided()
            => Assert.Equal(10, new SpiffyPoolOptions(0, 10).MaxBufferThreshold);

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Ctor_ShouldSetMaxBufferCountToDefault_When_NumberIsZeroOrLess(int value)
            => Assert.Equal(SpiffyPoolOptions.DefaultBufferThreshold, new SpiffyPoolOptions(value).MaxBufferThreshold);
    }
}
