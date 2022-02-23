using Devices.Core.State.Interfaces;

namespace Devices.Core.State.Visitors
{
    internal interface IStateControllerVisitor<TVisitableController, TVisitorAcceptor> where TVisitableController : ISubWorkflowHook
    {
        void Visit(TVisitableController context, TVisitorAcceptor visitorAcceptor);
    }
}
