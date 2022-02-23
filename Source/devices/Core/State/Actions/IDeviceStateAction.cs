using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System;
using System.Threading.Tasks;

namespace Devices.Core.State.Actions
{
    internal interface IDeviceStateAction : IActionReceiver, IDisposable
    {
        StateException LastException { get; }
        IDeviceStateController Controller { get; }
        DeviceWorkflowState WorkflowStateType { get; }
        void SetState(object stateObject);
        DeviceWorkflowStopReason StopReason { get; }
        bool DoDeviceDiscovery();
        Task DoWork();
    }
}
