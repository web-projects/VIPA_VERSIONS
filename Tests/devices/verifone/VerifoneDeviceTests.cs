using Common.Execution;
using Common.XO.Device;
using Common.XO.Private;
using Common.XO.Requests;
using Common.XO.Responses;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Config;
using Devices.Common.Helpers;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.Tests.Helpers;
using Devices.Verifone.VIPA;
using Devices.Verifone.VIPA.Helpers;
using Devices.Verifone.VIPA.Interfaces;
using Moq;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TestHelper;
using Xunit;
using static Common.XO.Responses.LinkEventResponse;
using static Devices.Verifone.VIPA.VIPAImpl;
using static XO.ProtoBuf.LogMessage.Types;
using EventCodeType = Common.XO.Responses.LinkEventResponse.EventCodeType;

namespace Devices.Verifone.Tests
{
    public class VerifoneDeviceTests
    {
        readonly VerifoneDevice subject;

        readonly DeviceConfig deviceConfig;
        readonly DeviceInformation deviceInformation;
        readonly SerialDeviceConfig serialConfig;

        Mock<IVipa> mockIVipa;

        List<EventCodeType> EventsSeen;
        List<string> MonitorMessagesSeen = new List<string>();
        List<LogLevel> DeviceLogEventSeen;

        public VerifoneDeviceTests()
        {
            mockIVipa = new Mock<IVipa>();

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
                VendorIdentifier = "BADDCACA",
                FirmwareVersion = "1.2.3"
            };

            subject = new VerifoneDevice();

            deviceConfig = new DeviceConfig()
            {
                Valid = true,
                SupportedTransactions = new SupportedTransactions()
                {
                    EnableContactEMV = false,
                    EnableContactlessEMV = false,
                    EnableContactlessMSR = true,
                    ContactEMVConfigIsValid = true,
                    EMVKernelValidated = true
                }
            };
            Helper.SetFieldValueToInstance<DeviceConfig>("deviceConfiguration", false, false, subject, deviceConfig);

            subject.AppExecConfig = new Execution.AppExecConfig()
            {
                //ExecutionMode = Modes.Execution.StandAlone
            };

            LinkDALRequestIPA5Object vipaVersions = new LinkDALRequestIPA5Object()
            {
                DALCdbData = new DALCDBData()
                {
                    VIPAVersion = new DALBundleVersioning() { DateCode = "1234" },
                    EMVVersion = new DALBundleVersioning() { DateCode = "1234" },
                    IdleVersion = new DALBundleVersioning() { DateCode = "1234" }
                }
            };
            Helper.SetPropertyValueToInstance<LinkDALRequestIPA5Object>("VipaVersions", false, false, subject, vipaVersions);

            using (IKernel kernel = new StandardKernel())
            {
                kernel.Settings.InjectNonPublic = true;
                kernel.Settings.InjectParentPrivateProperties = true;

                kernel.Bind<IVipa>().ToConstant(mockIVipa.Object).WithConstructorArgument(deviceInformation);
                kernel.Inject(subject);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Probe_ReturnsProperActiveState_WhenCalled(bool expectedValue)
        {
            mockIVipa.Setup(e => e.Connect(It.IsAny<VerifoneConnection>(), It.IsAny<DeviceInformation>())).Returns(expectedValue);

            DeviceConfig deviceConfig = new DeviceConfig()
            {
                Valid = true
            };

            subject.Probe(deviceConfig, deviceInformation, out bool actualValue);
            Assert.Equal(expectedValue, actualValue);
        }

        #region --- Helper Methods ---
        private bool SetVIPAMockConnectionForMockedDevice(IVipa vipaDevice)
        {
            LinkDeviceResponse deviceInfo = new LinkDeviceResponse()
            {
                Manufacturer = deviceInformation.Manufacturer,
                Model = string.IsNullOrEmpty(deviceInformation.Model) ? "ANYMODEL" : deviceInformation.Model,
                SerialNumber = deviceInformation.SerialNumber,
                FirmwareVersion = "1.2.3.0"
            };
            DeviceInfoObject deviceInfoObject = new DeviceInfoObject()
            {
                LinkDeviceResponse = deviceInfo
            };
            (DeviceInfoObject deviceInfoObject, int VipaResult) expectedValue = (deviceInfoObject, (int)VipaSW1SW2Codes.Success);

            Helper.SetPropertyValueToInstance<IVipa>("VipaDevice", true, false, subject, vipaDevice);
            Helper.SetPropertyValueToInstance<IVipa>("VipaConnection", false, false, subject, vipaDevice);
            //mockIVipa.Setup(e => e.ResetDevice()).Returns(expectedValue);
            //mockIVipa.Setup(e => e.GetDeviceHealth(It.IsAny<SupportedTransactions>())).Returns(expectedValue);
            //mockIVipa.Setup(e => e.GetDeviceInfo()).Returns(expectedValue);
            //mockIVipa.Setup(e => e.ConnectionConfiguration(It.IsAny<SerialDeviceConfig>(), It.IsAny<DeviceEventHandler>(), It.IsAny<DeviceLogHandler>()));
            mockIVipa.Setup(e => e.Connect(It.IsAny<VerifoneConnection>(), deviceInformation)).Returns(true);

            (DeviceInfoObject deviceInfoObject, int VipaResult) linkDeviceResponseValue = SetDeviceVIPAInfo();
            Assert.NotNull(linkDeviceResponseValue.deviceInfoObject);

            Assert.Null(subject.Probe(deviceConfig, deviceInformation, out bool active));
            return active;
        }

        private (DeviceInfoObject deviceInfoObject, int VipaResult) SetDeviceVIPAInfo()
        {
            LinkDeviceResponse deviceInfo = new LinkDeviceResponse()
            {
                Manufacturer = subject.ManufacturerConfigID,
                Model = subject.Name,
                SerialNumber = "DEADBEEF",
                FirmwareVersion = "1.2.3.4"
            };
            DeviceInfoObject deviceInfoObject = new DeviceInfoObject()
            {
                LinkDeviceResponse = deviceInfo
            };
            (DeviceInfoObject deviceInfoObject, int) linkDeviceResponseValue = (deviceInfoObject, (int)VipaSW1SW2Codes.Success);
            Helper.SetFieldValueToInstance<(DeviceInfoObject deviceInfoObject, int VipaResponse)>("deviceVIPAInfo", false, false, subject, linkDeviceResponseValue);
            return linkDeviceResponseValue;
        }
        #endregion --- Helper Methods ---

        #region --- Event Testing Functions ---

        private void Subject_PublishEvent(EventTypeType eventType, EventCodeType eventCode, List<LinkDeviceResponse> devices, object request, string message)
        {
            if (EventsSeen == null)
                EventsSeen = new List<EventCodeType>();
            EventsSeen.Add(eventCode);
        }

        private void Subject_PublishMonitor(EventTypeType eventType, EventCodeType eventCode, List<LinkDeviceResponse> devices, object request, string message)
        {
            MonitorMessagesSeen.Add(message);
        }

        public bool EventWasHandled(EventCodeType eventCode)
        {
            return (EventsSeen == null) ? false : EventsSeen.Contains(eventCode);
        }

        public bool MonitorMessageWasHandled(string message)
        {
            return MonitorMessagesSeen.Contains(message);
        }

        private DeviceEvent lastDeviceEvent;

        internal object DeviceEventHandlerStub(DeviceEvent deviceEvent, DeviceInformation deviceInformation)
        {
            lastDeviceEvent = deviceEvent;
            return false;
        }

        #endregion --- Event Testing Functions ---
    }
}
