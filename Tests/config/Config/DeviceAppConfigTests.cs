using Common.Config;
using Devices.Common.Helpers;
using System;
using Xunit;

namespace Config.Tests
{
    public class DeviceAppConfigTests
    {
        readonly DeviceAppConfig subject;

        public DeviceAppConfigTests()
        {
            subject = new DeviceAppConfig();
        }

        [Fact]
        public void SetServicerType_When_ServicerTypeProvided()
        {
            const string expectedValue = "Mock";
            string actualValue = subject.SetDeviceProvider(expectedValue).DeviceProvider.ToString();
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void SetServicerType_ThrowWhen_UnknownType()
        {
            string randomString = RandomGenerator.BuildRandomString(6);
            Assert.Throws<ArgumentException>(() => subject.SetDeviceProvider(randomString).DeviceProvider);
        }
    }
}
