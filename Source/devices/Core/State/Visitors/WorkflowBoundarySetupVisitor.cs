using Devices.Core.State.Interfaces;
using Devices.Core.State.SubWorkflows;

namespace Devices.Core.State.Visitors
{
    internal class WorkflowBoundarySetupVisitor : IStateControllerVisitor<ISubWorkflowHook, IDeviceSubStateController>
    {
        public void Visit(ISubWorkflowHook context, IDeviceSubStateController visitorAcceptor) => context.Hook(visitorAcceptor);
    }
}
