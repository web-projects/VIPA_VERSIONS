using Common.Execution;
using Common.XO.Private;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Verifone.Connection;
using Devices.Verifone.Helpers;
using Devices.Verifone.VIPA.Helpers;
using Devices.Verifone.VIPA.TagLengthValue;
using System;
using System.Collections.Generic;
using static Devices.Verifone.VIPA.VIPAImpl;

namespace Devices.Verifone.VIPA.Interfaces
{
    public interface IVipa : IDisposable
    {
        DeviceInformation DeviceInformation { get; }

        bool Connect(VerifoneConnection connection, DeviceInformation deviceInformation);

        void ConnectionConfiguration(SerialDeviceConfig serialConfig, DeviceEventHandler deviceEventHandler, DeviceLogHandler deviceLogHandler);

        void ResponseCodeHandler(List<TLV> tags, int responseCode, bool cancelled = false);

        (DeviceInfoObject deviceInfoObject, int VipaResponse) DeviceCommandReset();

        public bool DisplayMessage(VIPADisplayMessageValue displayMessageValue = VIPADisplayMessageValue.Idle, bool enableBacklight = false, string customMessage = "");

        (LinkDALRequestIPA5Object LinkActionRequestIPA5Object, int VipaResponse) DisplayCustomScreenHTML(string displayMessage);

        LinkDALRequestIPA5Object VIPAVersions(string deviceModel, bool hmacEnabled, string activeCustomerId);
    }
}