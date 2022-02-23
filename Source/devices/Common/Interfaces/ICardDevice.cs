using Common.XO.Requests;
using Common.XO.Responses;
using Devices.Common.AppConfig;
using Execution;
using System;
using System.Collections.Generic;
using static Common.XO.Responses.LinkEventResponse;

namespace Devices.Common.Interfaces
{
    public delegate void PublishEvent(EventTypeType eventType, EventCodeType eventCode,
            List<LinkDeviceResponse> devices, object request, string message);

    public interface ICardDevice : ICloneable, IDisposable
    {
        event PublishEvent PublishEvent;
        event DeviceEventHandler DeviceEventOccured;

        string Name { get; }

        string ManufacturerConfigID { get; }

        int SortOrder { get; set; }

        AppExecConfig AppExecConfig { get; set; }

        DeviceInformation DeviceInformation { get; }

        bool IsConnected(object request);

        void SetDeviceSectionConfig(DeviceSection config, AppExecConfig appConfig, bool displayOutput);

        List<DeviceInformation> DiscoverDevices();

        List<LinkErrorValue> Probe(DeviceConfig config, DeviceInformation deviceInfo, out bool dalActive);

        void DeviceSetIdle();

        bool DeviceRecovery();

        void Disconnect();

        List<LinkRequest> GetDeviceResponse(LinkRequest deviceInfo);

        // ------------------------------------------------------------------------
        // Methods that are mapped for usage in their respective sub-workflows.
        // ------------------------------------------------------------------------

        LinkRequest DisplayIdleScreen(LinkRequest linkRequest);

        LinkActionRequest ReportVipaVersions(LinkActionRequest linkActionRequest);
    }
}
