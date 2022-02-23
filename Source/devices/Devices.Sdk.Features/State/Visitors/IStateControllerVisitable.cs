namespace Devices.Sdk.Features.State.Visitors
{
    public interface IStateControllerVisitable<TVisitableController, TVisitorAcceptor>
        where TVisitableController : ISubWorkflowHook
        where TVisitorAcceptor : IDALSubStateController
    {
        void Accept(IStateControllerVisitor<TVisitableController, TVisitorAcceptor> visitor);
    }
}
