using Devices.Common;
using Devices.Sdk.Features.Internal.State;
using Common.XO.ProtoBuf;
using LinkRequest = XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.State
{
    public delegate void OnRequestReceived(CommunicationHeader header, LinkRequest request);
    public delegate void OnWorkflowStopped(DALWorkflowStopReason reason);
    public delegate void OnStateChange(DALWorkflowState oldState, DALWorkflowState newState);

    public interface IDALStateEventEmitter
    {
        event OnRequestReceived RequestReceived;
        event OnWorkflowStopped WorkflowStopped;
        event OnStateChange StateChange;
        DeviceLogHandler DeviceLogReceived { get; set; }
        DeviceEventHandler DeviceEventReceived { get; set; }
        ComPortEventHandler ComPortEventReceived { get; set; }
    }
}
