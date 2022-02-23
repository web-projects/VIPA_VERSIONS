using Devices.Sdk.Features.State.Visitors;

namespace Devices.Sdk.Features.State.Providers
{
    public interface IControllerVisitorProvider
    {
        IStateControllerVisitor<ISubWorkflowHook, IDALSubStateController> CreateBoundarySetupVisitor();
        IStateControllerVisitor<ISubWorkflowHook, IDALSubStateController> CreateBoundaryTeardownVisitor();
    }
}
