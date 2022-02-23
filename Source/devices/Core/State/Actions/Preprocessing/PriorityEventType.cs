namespace Devices.Core.State.Actions.Preprocessing
{
    public enum PriorityEventType : int
    {
        Undefined = 0,
        CommEvent,
        CancelationRequest,
        Timeout,
        UserCancel,
        DeviceReport
    }
}
