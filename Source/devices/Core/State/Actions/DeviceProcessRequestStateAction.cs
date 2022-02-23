using Common.XO.Device;
using Common.XO.Requests;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using Devices.Core.State.SubWorkflows;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Devices.Core.State.Actions
{
    internal class DeviceProcessRequestStateAction : DeviceBaseStateAction
    {
        public override DeviceWorkflowState WorkflowStateType => DeviceWorkflowState.ProcessRequest;

        public DeviceProcessRequestStateAction(IDeviceStateController _) : base(_) { }

        public override async Task DoWork()
        {
            // TODO: Interpret what type of object is here so that we can handle it accordingly.
            /**
             * if (StateObject is LinkRequest)
             * if (StateObject is DeviceEvent)
             **/
            if (StateObject != null)
            {
                await ProcessListenerRequest(StateObject as LinkRequest);
            }

            _ = Complete(this);
        }

        private async Task ProcessListenerRequest(LinkRequest linkRequest)
        {
            try
            {
                //_ = Controller.LoggingClient.LogInfoAsync($"Received from {linkRequest}");
                Debug.WriteLine($"Received from {linkRequest}");
                LinkRequest linkRequestResponse = null;
                bool deviceConnected = false;

                // Setup workflow
                WorkflowOptions workflowOptions = new WorkflowOptions();
                workflowOptions.StateObject = linkRequest;

                Controller.SaveState(workflowOptions);

                // Payment: targeted device
                if (linkRequest.Actions?[0]?.Action == LinkAction.Payment ||
                    linkRequest.Actions?[0]?.Action == LinkAction.DALAction)
                {
                    LinkDeviceIdentifier deviceIdentifier = linkRequest.GetDeviceIdentifier();
                    ICardDevice cardDevice = FindTargetDevice(deviceIdentifier);
                    if (cardDevice != null)
                    {
                        deviceConnected = cardDevice.IsConnected(linkRequest);
                        if (deviceConnected)
                        {
                            workflowOptions.StateObject = linkRequest;
                        }
                    }
                }
                else
                {
                    // Session: device discovery
                    foreach (var device in Controller.TargetDevices)
                    {
                        deviceConnected = device.IsConnected(linkRequest);
                        if (deviceConnected)
                        {
                            workflowOptions.StateObject = linkRequest;
                        }
                    }
                }

                if (deviceConnected)
                {
                    Controller.SaveState(workflowOptions);
                }
                else
                {
                    //linkRequestResponse = RequestHelper.LinkRequestResponseError(linkRequest, ErrorCodes.NoDevice, ErrorDescriptions.DeviceNotFound);

                    object response = JsonConvert.SerializeObject(linkRequestResponse);
                    //_ = Controller.LoggingClient.LogInfoAsync($"Sending to Listener");
                    Debug.WriteLine($"Sending to Listener");
                    //await Controller.Connector.Publish(response, new TopicOption[] { TopicOption.Servicer }).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                //_ = Controller.LoggingClient.LogErrorAsync($"{e.Message} {linkRequest}");
                Console.WriteLine($"{e.Message} {linkRequest}");
                //if (Controller.Connector != null)
                //{
                //    await Controller.Connector.Publish(RequestHelper.LinkRequestResponseError(null, ErrorCodes.DalError, e.Message), new TopicOption[] { TopicOption.Servicer }).ConfigureAwait(false);
                //}
            }
        }
    }
}