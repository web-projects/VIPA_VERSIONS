using Common.XO.Requests;
using Devices.Common;
using Devices.Core.State.Enums;

namespace Devices.Core.State.Interfaces
{
    public delegate void OnRequestReceived(LinkRequest request);
    public delegate void OnWorkflowStopped(DeviceWorkflowStopReason reason);
    public delegate void OnStateChange(DeviceWorkflowState oldState, DeviceWorkflowState newState);

    public interface IDeviceStateEventEmitter
    {
        event OnRequestReceived RequestReceived;
        event OnWorkflowStopped WorkflowStopped;
        event OnStateChange StateChange;
        DeviceEventHandler DeviceEventReceived { get; set; }
        ComPortEventHandler ComPortEventReceived { get; set; }
    }
}
