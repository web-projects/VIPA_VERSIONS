using Devices.Common;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Helpers;
using Devices.Core.State.Enums;
using Devices.Core.State.SubWorkflows.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Common.XO.Device;
using Common.XO.Requests;

namespace Devices.Core.State.SubWorkflows.Actions
{
    internal abstract class DeviceBaseSubStateAction : IDeviceSubStateAction
    {
        public virtual bool WorkflowCutoff { get; }
        public StateException LastException { get; set; }

        public IDeviceSubStateController Controller { get; }

        public abstract DeviceSubWorkflowState WorkflowStateType { get; }

        public object StateObject { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public virtual SubStateActionLaunchRules LaunchRules { get; }

        public DeviceBaseSubStateAction(IDeviceSubStateController controller) => (Controller) = (controller);

        public void Dispose()
        {
            /* Empty for now */
        }

        public virtual Task DoWork()
        {
            if (StateObject != null)
            {
                Controller.SaveState(StateObject);
            }

            _ = Complete(this);

            return Task.CompletedTask;
        }

        public void SetState(object stateObject) => (StateObject) = (stateObject);

        public void SetCancellationToken(CancellationToken cancellationToken) => (CancellationToken) = (cancellationToken);

        public virtual void RequestReceived(object request)
        {

        }

        public virtual object DeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation)
        {
            if (deviceEvent == DeviceEvent.CancelKeyPressed)
            {
                //_ = Controller.LoggingClient.LogInfoAsync($"device event received: {deviceEvent} from SN: {deviceInformation.SerialNumber}");
                Console.WriteLine($"device event received: {deviceEvent} from SN: {deviceInformation.SerialNumber}");
            }

            return false;
        }

        public virtual Task ComportEventReceived(PortEventType comPortEvent, string portNumber)
        {
            if (comPortEvent == PortEventType.Removal)
            {
                //_ = Controller.LoggingClient.LogInfoAsync($"device event received: {comPortEvent} on port: {portNumber}");
                Console.WriteLine($"device event received: {comPortEvent} on port: {portNumber}");
            }

            return Task.CompletedTask;
        }

        public void BuildSubworkflowErrorResponse(LinkRequest linkRequest, DeviceInformation deviceInformation, DeviceEvent deviceEvent)
        {
            ResponseBuilder.SubworkflowErrorResponse(deviceInformation, linkRequest,
                linkRequest.Actions[0].Action.ToString(), DeviceEventDecoder.GetDeviceEvent(deviceEvent), StringValueAttribute.GetStringValue(deviceEvent));
        }

        public ICardDevice FindTargetDevice(LinkDeviceIdentifier deviceIdentifier)
        {
            if (deviceIdentifier == null)
            {
                return null;
            }

            return FindMatchingDevice(deviceIdentifier);
        }

        private protected ICardDevice FindMatchingDevice(LinkDeviceIdentifier deviceIdentifier)
        {
            ICardDevice cardDevice = null;

            foreach (var device in Controller.TargetDevices)
            {
                if (device.DeviceInformation != null)
                {
                    if (device.DeviceInformation.Manufacturer.Equals(deviceIdentifier.Manufacturer, StringComparison.CurrentCultureIgnoreCase) &&
                        device.DeviceInformation.Model.Equals(deviceIdentifier.Model, StringComparison.CurrentCultureIgnoreCase) &&
                        device.DeviceInformation.SerialNumber.Equals(deviceIdentifier.SerialNumber, StringComparison.CurrentCultureIgnoreCase))
                    {
                        cardDevice = device;
                        break;
                    }
                }
            }

            return cardDevice;
        }

        private protected void UpdateRequestDeviceNotFound(LinkRequest linkRequest, LinkDeviceIdentifier deviceIdentifier)
        {
            if (deviceIdentifier != null)
            {
                DeviceInformation deviceInformation = new DeviceInformation()
                {
                    Manufacturer = deviceIdentifier?.Manufacturer,
                    Model = deviceIdentifier?.Model,
                    SerialNumber = deviceIdentifier?.SerialNumber,
                };
                //_ = Controller.LoggingClient.LogErrorAsync($"Unable to obtain device information from request - '{DeviceDiscovery.NoDeviceMatched}'.");
                Console.WriteLine($"Unable to obtain device information from request - '{DeviceDiscovery.NoDeviceMatched}'.");
                BuildSubworkflowErrorResponse(linkRequest, deviceInformation, Controller.DeviceEvent);
            }
            else
            {
                //_ = Controller.LoggingClient.LogErrorAsync($"Unable to obtain device information from request - '{DeviceDiscovery.NoDeviceSpecified}'.");
                Console.WriteLine($"Unable to obtain device information from request - '{DeviceDiscovery.NoDeviceSpecified}'.");
                BuildSubworkflowErrorResponse(linkRequest, null, Controller.DeviceEvent);
            }
        }

        protected Task Complete(IDeviceSubStateAction state) => _ = Task.Run(() => Controller.Complete(state));

        protected Task Error(IDeviceSubStateAction state) => _ = Task.Run(() => Controller.Error(state));

        public virtual void RequestReceived(LinkRequest request)
        {

        }
    }
}
