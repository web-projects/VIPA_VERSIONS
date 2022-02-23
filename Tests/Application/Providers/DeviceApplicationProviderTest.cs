using System;
using Xunit;

namespace DEVICE_CORE.Providers.Tests
{
    public class DeviceApplicationProviderTest
    {
        readonly DeviceApplicationProvider subject;

        public DeviceApplicationProviderTest()
        {
            subject = new DeviceApplicationProvider();
        }

        [Fact]
        public void GetDeviceApplication_ShouldReturnValidDALApplication_When_Called()
        {
            Type expectedType = typeof(DeviceApplication);

            IDeviceApplication application = subject.GetDeviceApplication();

            Assert.IsAssignableFrom(expectedType, application);
        }
    }
}
