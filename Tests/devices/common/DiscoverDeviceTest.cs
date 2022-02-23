using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Verifone;
using Moq;
using Ninject;
using System;
using Xunit;

namespace Devices.Common.Tests
{
    public class DiscoverDeviceTests
    {
        readonly DiscoverDevice subject;
        readonly Mock<IDeviceProvider> mockDeviceProvider;

        public DiscoverDeviceTests()
        {
            subject = new DiscoverDevice();

            mockDeviceProvider = new Mock<IDeviceProvider>();

            using (IKernel kernel = new StandardKernel())
            {
                kernel.Settings.InjectNonPublic = true;
                kernel.Settings.InjectParentPrivateProperties = true;

                kernel.Bind<IDeviceProvider>().ToConstant(mockDeviceProvider.Object);
                kernel.Bind<DiscoverDevice>().ToSelf();

                kernel.Inject(subject);
            }
        }

        [Fact]
        public void DiscoverDevice_ShouldThrowException_WhenDeviceProviderReturnsNullDevice()
        {
            mockDeviceProvider.Setup(e => e.GetDevice(It.IsAny<string>())).Returns<ICardDevice>(null);
            Action subjectAction = () => subject.DiscoverDevices(null);
            Assert.Throws<Exception>(subjectAction);
        }

        [Fact]
        public void DiscoverDevice_ShouldReturnFalse_WhenIdTechDeviceProviderReturnsVerifoneDevice()
        {
            Devices.Common.Interfaces.ICardDevice device = new VerifoneDevice();
            mockDeviceProvider.Setup(e => e.GetDevice(It.IsAny<string>())).Returns(device);
            Assert.False(subject.DiscoverDevices(StringValueAttribute.GetStringValue(DeviceType.IdTech)));
        }

        [Fact]
        public void DiscoverDevice_ShouldReturnTrue_WhenVerifoneDeviceProviderIsVerifone()
        {
            Devices.Common.Interfaces.ICardDevice device = new VerifoneDevice();
            mockDeviceProvider.Setup(e => e.GetDevice(It.IsAny<string>())).Returns(device);
            Assert.True(subject.DiscoverDevices(StringValueAttribute.GetStringValue(DeviceType.Verifone)));
        }
    }
}
