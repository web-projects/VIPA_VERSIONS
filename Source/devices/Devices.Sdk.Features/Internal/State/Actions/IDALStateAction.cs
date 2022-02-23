using Devices.Common.State;
using Devices.Sdk.Features.State;
using System;
using System.Threading.Tasks;

namespace Devices.Sdk.Features.Internal.State.Actions
{
    internal interface IDALStateAction : IActionReceiver, IDisposable
    {
        StateException LastException { get; }
        IDALStateController Controller { get; }
        DALWorkflowState WorkflowStateType { get; }
        void SetState(object stateObject);
        DALWorkflowStopReason StopReason { get; }
        bool DoDeviceDiscovery();
        Task DoWork();
    }
}
