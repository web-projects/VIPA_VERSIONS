using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Devices.Core.State.Actions
{
    internal class DeviceInitializeDeviceHealthStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.InitializeDeviceHealth;

        public DeviceInitializeDeviceHealthStateAction(IDeviceStateController _) : base(_) { }

        public override Task DoWork()
        {
            // TODO: Implement Device Health here.
            //Controller.LoggingClient?.LogInfoAsync($"Currently in the '{WorkflowStateType}' state with nothing to do.. skipping...");
            Debug.WriteLine($"Currently in the '{WorkflowStateType}' state with nothing to do.. skipping...");

            Controller.SetPublishEventHandlerAsTask();

            _ = Complete(this);

            return Task.CompletedTask;
        }
    }
}
