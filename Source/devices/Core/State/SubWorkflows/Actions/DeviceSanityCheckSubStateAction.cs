using Common.XO.Device;
using Common.XO.Requests;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.Helpers;
using Devices.Core.State.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Devices.Core.State.Enums.DeviceSubWorkflowState;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal class DeviceSanityCheckSubStateAction : DeviceBaseSubStateAction
    {
        public override DeviceSubWorkflowState WorkflowStateType => SanityCheck;

        public DeviceSanityCheckSubStateAction(IDeviceSubStateController _) : base(_) { }

        private async Task RecoverDevice(LinkRequest linkRequest, IDeviceCancellationBroker cancellationBroker)
        {
            LinkDeviceIdentifier deviceIdentifier = linkRequest.GetDeviceIdentifier();
            ICardDevice cardDevice = FindTargetDevice(deviceIdentifier);

            if (cardDevice != null)
            {
                var timeoutPolicy = await cancellationBroker.ExecuteWithTimeoutAsync<bool>(
                                    _ => cardDevice.DeviceRecovery(),
                                    DeviceConstants.DeviceRecoveryTimeout,
                                    this.CancellationToken);

                if (timeoutPolicy.Outcome == Polly.OutcomeType.Failure)
                {
                    //_ = Controller.LoggingClient.LogErrorAsync("Unable to recover device.");
                    Console.WriteLine("Unable to recover device.");
                }
            }
        }

        public async override Task DoWork()
        {
            if (Controller.DidTimeoutOccur || Controller.DeviceEvent != DeviceEvent.None)
            {
                // recover device to idle
                IDeviceCancellationBroker cancellationBroker = Controller.GetDeviceCancellationBroker();

                LinkRequest linkRequest = StateObject as LinkRequest;

                if (linkRequest is { })
                {
                    await RecoverDevice(linkRequest, cancellationBroker);
                }
                else
                {
                    List<LinkRequest> linkRequests = StateObject as List<LinkRequest>;

                    foreach(LinkRequest request in linkRequests)
                    {
                        await RecoverDevice(request, cancellationBroker);
                    }
                }
            }

            if (StateObject != null)
            {
                Controller.SaveState(StateObject);
            }

            _ = Complete(this);
        }
    }
}
