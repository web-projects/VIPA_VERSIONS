using Devices.Common;
using Devices.Simulator.Connection;
using Moq;
using Ninject;
using Common.XO.Requests;
using Xunit;

namespace Devices.Simulator.Tests
{
    public class DeviceSimulatorTests
    {
        readonly DeviceSimulator subject;

        readonly DeviceInformation deviceInformation;
        readonly SerialDeviceConfig serialConfig;

        Mock<ISerialConnection> moqSerialConnection;

        public DeviceSimulatorTests()
        {
            moqSerialConnection = new Mock<ISerialConnection>();

            serialConfig = new SerialDeviceConfig
            {
                CommPortName = "COM9"
            };

            deviceInformation = new DeviceInformation()
            {
                ComPort = "COM9",
                Manufacturer = "Simulator",
                Model = "SimCity",
                SerialNumber = "CEEEDEADBEEF",
                ProductIdentification = "SIMULATOR",
                VendorIdentifier = "BADDCACA"
            };

            subject = new DeviceSimulator();

            using (IKernel kernel = new StandardKernel())
            {
                kernel.Settings.InjectNonPublic = true;
                kernel.Settings.InjectParentPrivateProperties = true;

                kernel.Bind<ISerialConnection>().ToConstant(moqSerialConnection.Object).WithConstructorArgument(deviceInformation);
                kernel.Inject(subject);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Probe_ReturnsProperActiveState_WhenCalled(bool expectedValue)
        {
            moqSerialConnection.Setup(e => e.Connect(It.IsAny<string>(), false)).Returns(expectedValue);

            DeviceConfig deviceConfig = new DeviceConfig()
            {
                Valid = true
            };
           
            deviceConfig.SetSerialDeviceConfig(serialConfig);

            subject.Probe(deviceConfig, deviceInformation, out bool actualValue);
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void GetStatus_ReturnsExpectedValue_WhenCalled()
        {
            LinkRequest expectedValue = new LinkRequest();
            LinkRequest actualValue = subject.GetStatus(expectedValue);
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
