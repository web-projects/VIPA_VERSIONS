using Devices.Common.AppConfig;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.State.SubWorkflows.Actions;
using System.Collections.Generic;

namespace Devices.Core.State.SubWorkflows
{
    internal interface IDeviceSubStateController : IStateControlTrigger<IDeviceSubStateAction>
    {
        DeviceSection Configuration { get; }
        //ILoggingServiceClient LoggingClient { get; }
        //IListenerConnector Connector { get; }
        List<ICardDevice> TargetDevices { get; }
        bool DidTimeoutOccur { get; }
        public DeviceEvent DeviceEvent { get; }
        void SaveState(object stateObject);
        IDeviceCancellationBroker GetDeviceCancellationBroker();
    }
}
