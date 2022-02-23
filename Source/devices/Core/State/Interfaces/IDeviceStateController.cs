using Common.Core.Patterns.Queuing;
using Devices.Common.AppConfig;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.SerialPort.Interfaces;
using Devices.Core.State.Actions;
using Devices.Core.State.Actions.Preprocessing;
using Devices.Core.State.Providers;
using Devices.Sdk;
using Execution;
using System.Collections.Generic;

namespace Devices.Core.State.Interfaces
{
    internal interface IDeviceStateController : IDeviceStateEventEmitter, IStateControlTrigger<IDeviceStateAction>
    {
        string PluginPath { get; }
        DeviceSection Configuration { get; }
        AppExecConfig AppExecConfig { get; }
        IDevicePluginLoader DevicePluginLoader { get; set; }
        List<ICardDevice> TargetDevices { get; }
        ISerialPortMonitor SerialPortMonitor { get; }
        PriorityQueue<PriorityQueueDeviceEvents> PriorityQueue { get; set; }
        //ILoggingServiceClient LoggingClient { get; }
        //IListenerConnector Connector { get; }
        List<ICardDevice> AvailableCardDevices { get; }
        void SetTargetDevices(List<ICardDevice> targetDevices);
        void SetPublishEventHandlerAsTask();
        void SendDeviceCommand(object message);
        void SaveState(object stateObject);
        IControllerVisitorProvider GetCurrentVisitorProvider();
        ISubStateManagerProvider GetSubStateManagerProvider();
        IDeviceCancellationBroker GetCancellationBroker();
        void DeviceStatusUpdate();
        void PublishDeviceConnectEvent(ICardDevice device, string portNumber);
    }
}
