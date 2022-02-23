using IPA5.Core.Constants;
using Devices.Common;
using Devices.Common.Helpers;
using Devices.Common.State;
using Devices.Sdk.Features.Cancellation;
using Common.XO.Common.DAL;
using Common.XO.Enums.Legacy;
using Common.XO.Requests;
using System.Threading.Tasks;
using static Devices.Sdk.Features.State.DALSubWorkflowState;

namespace Devices.Sdk.Features.State.Actions
{
    internal class DALSanityCheckSubStateAction : DALBaseSubStateAction
    {
        public override DALSubWorkflowState WorkflowStateType => SanityCheck;

        public DALSanityCheckSubStateAction(IDALSubStateController _) : base(_) { }

        public async override Task DoWork()
        {
            if (Controller.Register.LastAsyncBrokerOutcome == LEBO.Failure ||
                Controller.DidTimeoutOccur ||
                Controller.DidCancellationOccur ||
                Controller.DeviceEvent != DeviceEvent.None)
            {
                // recover device to idle
                IDeviceCancellationBroker cancellationBroker = Controller.GetDeviceCancellationBroker();

                if (Controller.TargetDevice != null)
                {
                    var timeoutPolicy = await cancellationBroker.ExecuteWithTimeoutAsync<bool>(
                        _ => Controller.TargetDevice.DeviceRecovery(),
                        Timeouts.DALDeviceRecoveryTimeout,
                        CancellationToken);

                    if (timeoutPolicy.Outcome == Polly.OutcomeType.Failure)
                    {
                        _ = Controller.LoggingClient.LogErrorAsync("Unable to recover device.", StatusType.DALTimeOuts);
                    }
                }
                else
                {
                    CommunicationObject commObject = StateObject as CommunicationObject;
                    LinkRequest linkRequest = commObject?.LinkRequest;

                    if (linkRequest != null)
                    {
                        LinkDeviceIdentifier deviceIdentifier = linkRequest.GetDeviceIdentifier();

                        IPaymentDevice targetDevice = FindTargetDevice(deviceIdentifier);
                        if (targetDevice is ICardDevice cardDevice)
                        {
                            cardDevice.SetRequestHeader(commObject.Header);
                            var timeoutPolicy = await cancellationBroker.ExecuteWithTimeoutAsync<bool>(
                                _ => cardDevice.DeviceRecovery(),
                                Timeouts.DALDeviceRecoveryTimeout,
                                CancellationToken);

                            if (timeoutPolicy.Outcome == Polly.OutcomeType.Failure)
                            {
                                _ = Controller.LoggingClient.LogErrorAsync("Unable to recover device.", StatusType.DALTimeOuts);
                            }
                        }
                    }
                }
            }

            if (StateObject != null)
            {
                Controller.SaveState(StateObject as CommunicationObject);
            }

            _ = Complete(this);
        }
    }
}
