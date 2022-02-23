using Devices.Common.State;
using Common.XO.Common.Helpers;
using Common.XO.ProtoBuf;
using Common.XO.Requests.DAL;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using static Devices.Sdk.Features.State.DALSubWorkflowState;
using LinkRequest = XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.State.Actions
{
    internal class DALRequestCompleteSubStateAction : DALBaseSubStateAction
    {
        public override bool WorkflowCutoff => true;

        public override DALSubWorkflowState WorkflowStateType => RequestComplete;

        public DALRequestCompleteSubStateAction(IDALSubStateController _) : base(_) { }

        public override Task DoWork()
        {
            if (StateObject is CommunicationObject stateObject)
            {
                string serializedResponse = JsonConvert.SerializeObject(stateObject.LinkRequest);

                if (DALActionTypeHelper.IsActionTest(stateObject.LinkRequest.Actions?.FirstOrDefault()?.DALActionRequest?.DALAction))
                {
                    _ = Controller.Connector.PublishAsync(serializedResponse, stateObject.Header, stateObject.Header.CommIdentifiers[0].Service, stateObject.Header?.CommIdentifiers[0]).ConfigureAwait(false);
                }
                else
                {
                    _ = Controller.Connector.PublishAsync(serializedResponse, stateObject.Header, ServiceType.Servicer).ConfigureAwait(false);
                }
                _ = Controller.LoggingClient.LogInfoAsync($"Request completed. Sending to broker.");

            }
            else
            {
                // TODO: What should you do here in the event that you have no response?
                // TODO: Furthermore, this is a cutoff state action which means that you have
                // nowhere else to go when this completes.. This is the one point where you
                // get to decide whether or not we throw a Completion (Manage Path) or an
                // Error (Device Recovery Path). I'll leave it to you to decide what you'll expect
                // to receive in the state object to make that determination.
                //
                // Perhaps, the state object can be different in order to illuminate the type
                // of flow you'll take going forward.
            }

            Complete(this);

            return Task.CompletedTask;
        }
    }
}
