using Devices.Common.State;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Sdk.Features.State.Actions
{
    public interface IDALSubStateAction : IActionReceiver, IDisposable
    {
        bool WorkflowCutoff { get; }
        StateException LastException { get; }
        IDALSubStateController Controller { get; }
        DALSubWorkflowState WorkflowStateType { get; }
        CancellationToken CancellationToken { get; }
        SubStateActionLaunchRules LaunchRules { get; }
        void SetState(object stateObject);
        void SetCancellationToken(CancellationToken cancellationToken);
        Task DoWork();
    }
}
