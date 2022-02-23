using Devices.Core.State.Interfaces;
using Devices.Core.State.SubWorkflows;
using Devices.Core.State.Visitors;

namespace Devices.Core.State.Providers
{
    internal interface IControllerVisitorProvider
    {
        IStateControllerVisitor<ISubWorkflowHook, IDeviceSubStateController> CreateBoundarySetupVisitor();
        IStateControllerVisitor<ISubWorkflowHook, IDeviceSubStateController> CreateBoundaryTeardownVisitor();
    }
}
