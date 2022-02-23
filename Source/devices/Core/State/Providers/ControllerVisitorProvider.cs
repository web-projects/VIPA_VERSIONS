using Devices.Core.State.Interfaces;
using Devices.Core.State.SubWorkflows;
using Devices.Core.State.Visitors;

namespace Devices.Core.State.Providers
{
    internal class ControllerVisitorProvider : IControllerVisitorProvider
    {
        public IStateControllerVisitor<ISubWorkflowHook, IDeviceSubStateController> CreateBoundarySetupVisitor()
            => new WorkflowBoundarySetupVisitor();

        public IStateControllerVisitor<ISubWorkflowHook, IDeviceSubStateController> CreateBoundaryTeardownVisitor()
            => new WorkflowBoundaryTeardownVisitor();
    }
}
