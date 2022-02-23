using Devices.Core.State.Interfaces;
using Devices.Core.State.Visitors;
using System;

namespace Devices.Core.State.SubWorkflows.Management
{
    public delegate void OnSubWorkflowCompleted();
    public delegate void OnSubWorkflowError();

    internal interface IDeviceSubStateManager : IActionReceiver, IStateControllerVisitable<ISubWorkflowHook, IDeviceSubStateController>, IDisposable
    {
        void LaunchWorkflow(WorkflowOptions launchOptions);
        event OnSubWorkflowCompleted SubWorkflowComplete;
        event OnSubWorkflowError SubWorkflowError;
    }
}
