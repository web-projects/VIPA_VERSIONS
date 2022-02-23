using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System.Threading.Tasks;

namespace Devices.Core.State.Actions
{
    internal class DeviceNoneStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.None;

        public DeviceNoneStateAction(IDeviceStateController _) : base(_) { }

        public override Task DoWork()
        {
            _ = Complete(this);

            return Task.CompletedTask;
        }
    }
}
