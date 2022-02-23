using Devices.Core.State.Enums;

namespace Devices.Core.State.SubWorkflows.Actions.Controllers
{
    internal interface IDeviceSubStateActionController
    {
        IDeviceSubStateAction GetFinalState();
        IDeviceSubStateAction GetNextAction(IDeviceSubStateAction stateAction);
        IDeviceSubStateAction GetNextAction(DeviceSubWorkflowState state);
    }
}
