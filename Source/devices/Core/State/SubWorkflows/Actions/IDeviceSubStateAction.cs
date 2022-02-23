using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal interface IDeviceSubStateAction : IActionReceiver, IDisposable
    {
        bool WorkflowCutoff { get; }
        StateException LastException { get; }
        IDeviceSubStateController Controller { get; }
        DeviceSubWorkflowState WorkflowStateType { get; }
        CancellationToken CancellationToken { get; }
        SubStateActionLaunchRules LaunchRules { get; }
        void SetState(object stateObject);
        void SetCancellationToken(CancellationToken cancellationToken);
        Task DoWork();
    }
}
