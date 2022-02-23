using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;

namespace Devices.Core.State.Actions
{
    internal class DeviceShutdownStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.Shutdown;

        public DeviceShutdownStateAction(IDeviceStateController _) : base(_) { }
    }
}
