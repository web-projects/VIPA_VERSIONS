using Devices.Sdk.Features.State.Visitors;
using System;

namespace Devices.Sdk.Features.State.Management
{
    public interface IDALSubStateManager : IActionReceiver, IStateControllerVisitable<ISubWorkflowHook, IDALSubStateController>, IDisposable
    {
        void LaunchWorkflow(WorkflowOptions launchOptions);
        event OnSubWorkflowCompleted SubWorkflowComplete;
        event OnSubWorkflowError SubWorkflowError;
    }
}
