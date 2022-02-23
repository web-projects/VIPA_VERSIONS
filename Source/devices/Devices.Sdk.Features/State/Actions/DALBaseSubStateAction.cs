using Common.XO.Device;
using Common.XO.Private;
using Devices.Common;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Helpers;
using Devices.Core.State.SubWorkflows.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Common.XO.ProtoBuf.LogMessage.Types;
using static XO.ProtoBuf.LogMessage.Types;
using LinkActionResponse = Common.XO.Responses.LinkActionResponse;
using LinkErrorValue = Common.XO.Responses.LinkErrorValue;
using LinkRequest = Common.XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.State.Actions
{
    public abstract class DALBaseSubStateAction : IDeviceSubStateAction
    {
        public virtual bool WorkflowCutoff { get; }
        public StateException LastException { get; set; }

        public IDALSubStateController Controller { get; }

        public abstract DALSubWorkflowState WorkflowStateType { get; }

        public object StateObject { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public virtual SubStateActionLaunchRules LaunchRules { get; }

        public DALBaseSubStateAction(IDALSubStateController controller) => (Controller) = (controller);

        public virtual void Dispose()
        {

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

        //public virtual void RequestReceived(CommunicationHeader header, LinkRequest request)
        //{
        //}

        public virtual bool RequestSupported(LinkRequest request) => false;

        public virtual void InterruptRequestReceived(LinkRequest request)
        {

        }

        public virtual void DeviceLogReceived(LogLevel logLevel, string message)
        {

        }

        public virtual void DeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation)
        {
            if (deviceEvent == DeviceEvent.CancelKeyPressed)
            {
                _ = Controller.LoggingClient.LogInfoAsync($"device event received: {deviceEvent} from SN: {deviceInformation.SerialNumber}");
            }
        }

        public virtual Task ComportEventReceived(PortEventType comPortEvent, string portNumber)
        {
            if (comPortEvent == PortEventType.Removal)
            {
                _ = Controller.LoggingClient.LogInfoAsync($"device event received: {comPortEvent} on port: {portNumber}");
            }

            return Task.CompletedTask;
        }

        public void BuildSubworkflowErrorResponse(LinkRequest linkRequest, DeviceInformation deviceInformation, DeviceEvent deviceEvent, bool setDeviceError = true)
            => ResponseBuilder.SubworkflowErrorResponse(
                deviceInformation,
                linkRequest,
                linkRequest.Actions[0].Action.ToString(),
                DeviceEventDecoder.GetDeviceEvent(deviceEvent),
                StringValueAttribute.GetStringValue(deviceEvent),
                setDeviceError);

        public LinkErrorValue CreateSubworkflowError(LinkRequest linkRequest, DeviceEvent deviceEvent) =>
            new LinkErrorValue()
            {
                Type = linkRequest.Actions[0].Action.ToString(),
                Code = DeviceEventDecoder.GetDeviceEvent(deviceEvent),
                Description = StringValueAttribute.GetStringValue(deviceEvent)
            };

        public IPaymentDevice FindTargetDevice(LinkDeviceIdentifier deviceIdentifier) => TargetDeviceHelper.FindTargetDevice(
            deviceIdentifier,
            Controller.GetAvailableDevices()
        );

        protected void UpdateRequestDeviceNotFound(XO.Requests.LinkRequest linkRequest, LinkDeviceIdentifier deviceIdentifier)
        {
            if (deviceIdentifier != null)
            {
                DeviceInformation deviceInformation = new DeviceInformation()
                {
                    Manufacturer = deviceIdentifier?.Manufacturer,
                    Model = deviceIdentifier?.Model,
                    SerialNumber = deviceIdentifier?.SerialNumber,
                };
                _ = Controller.LoggingClient.LogErrorAsync($"Unable to obtain device information from request - '{DeviceDiscovery.NoDeviceMatched}'.");
                BuildSubworkflowErrorResponse(linkRequest, deviceInformation, Controller.DeviceEvent);
            }
            else
            {
                _ = Controller.LoggingClient.LogErrorAsync($"Unable to obtain device information from request - '{DeviceDiscovery.NoDeviceSpecified}'.");
                BuildSubworkflowErrorResponse(linkRequest, null, Controller.DeviceEvent);
            }
        }

        protected Task Complete(IDALSubStateAction state) => _ = Task.Run(() => Controller.Complete(state));

        protected Task Error(IDALSubStateAction state) => _ = Task.Run(() => Controller.Error(state));

        protected private void AdaMessageResponse(LinkRequest request, string message, string value)
        {
            if (request.LinkObjects == null)
            {
                request.LinkObjects = new LinkRequestIPA5Object();
            }
            if (request.LinkObjects.LinkActionResponseList == null)
            {
                request.LinkObjects.LinkActionResponseList = new List<LinkActionResponse>();
            }
            if (request.LinkObjects.LinkActionResponseList.Count == 0)
            {
                request.LinkObjects.LinkActionResponseList.Add(new LinkActionResponse());
            }
            if (request.LinkObjects.LinkActionResponseList[0].DALActionResponse == null)
            {
                request.LinkObjects.LinkActionResponseList[0].DALActionResponse = new LinkDALActionResponse();
            }

            request.LinkObjects.LinkActionResponseList[0].DALActionResponse.Status = message;
            request.LinkObjects.LinkActionResponseList[0].DALActionResponse.Value = value;
        }

    }
}
