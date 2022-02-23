using Devices.Common.Helpers;
using System;
using static Devices.Common.Helpers.EventResponses;

namespace Devices.Core.State.SubWorkflows.Helpers
{
    public static class DeviceEventDecoder
    {
        public static string GetDeviceEvent(DeviceEvent deviceEvent)
            => deviceEvent switch
            {
                DeviceEvent.CancellationRequest => Enum.GetName(typeof(EventCodeType), EventCodeType.CANCELLATION_REQUEST),
                DeviceEvent.DeviceUnplugged => Enum.GetName(typeof(EventCodeType), EventCodeType.DEVICE_UNPLUGGED),
                _ => Enum.GetName(typeof(EventCodeType), EventCodeType.REQUEST_TIMEOUT)
            };
    }
}
