using Devices.Core.State.Enums;

namespace Devices.Core.State.Actions.Controllers
{
    internal interface IDeviceStateActionController
    {
        IDeviceStateAction GetFinalState();
        IDeviceStateAction GetNextAction(IDeviceStateAction stateAction);
        IDeviceStateAction GetNextAction(DeviceWorkflowState state);
        IDeviceStateAction GetSpecificAction(DeviceWorkflowState state);
    }
}
