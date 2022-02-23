using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System.Threading.Tasks;

namespace Devices.Core.State.Actions
{
    internal class DeviceRecoveryStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.DeviceRecovery;

        public DeviceRecoveryStateAction(IDeviceStateController _) : base(_) { }

        public override async Task DoWork()
        {
            //TODO: read delay from configuration
            await Task.Delay(4096);

            _ = Complete(this);
        }
    }
}
