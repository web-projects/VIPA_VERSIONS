namespace Devices.Sdk.Features.State.Visitors
{
    public interface IStateControllerVisitor<TVisitableController, TVisitorAcceptor> 
        where TVisitableController : ISubWorkflowHook
    {
        void Visit(TVisitableController context, TVisitorAcceptor visitorAcceptor);
    }
}
