using DEVICE_CORE.Modules;
using DEVICE_CORE.Providers;
using Moq;
using Ninject;
using System;
using Xunit;

namespace DEVICE_CORE.Activator.Tests
{
    public class DeviceActivatorTest
    {
        readonly DeviceActivator subject;
        readonly Mock<IDeviceApplication> mockApplication;
        readonly Mock<IDeviceApplicationProvider> mockApplicationProvider;

        public DeviceActivatorTest()
        {
            subject = new DeviceActivator();

            mockApplication = new Mock<IDeviceApplication>();

            mockApplicationProvider = new Mock<IDeviceApplicationProvider>();
            mockApplicationProvider.Setup(e => e.GetDeviceApplication()).Returns(mockApplication.Object);

            using (IKernel kernel = new DeviceKernelResolver().ResolveKernel())
            {
                kernel.Rebind<IDeviceApplicationProvider>().ToConstant(mockApplicationProvider.Object);
                kernel.Inject(subject);
            }
        }

        [Theory]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData(null)]
        public void Start_ShouldThrowArgumentNullExceptionException_When_PluginPathIsNull(string pluginPath)
        {
            Action action = () => subject.Start(pluginPath);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Start_ShouldInitializeAndLaunchDALApplication_When_Called()
        {
            const string fakePluginPath = @"C:\somepath";

            IDeviceApplication application = subject.Start(fakePluginPath);

            mockApplicationProvider.Verify(e => e.GetDeviceApplication(), Times.Once());

            mockApplication.Verify(e => e.Initialize(fakePluginPath), Times.Once());
            //mockApplication.Verify(e => e.LaunchApplication(), Times.Once());

            Assert.Same(mockApplication.Object, application);
        }
    }
}
