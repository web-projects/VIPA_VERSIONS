using Devices.Common;
using Devices.Sdk.Features.Internal.State;
using IPA5.LoggingService.Client;
using Common.XO.Common.DAL;
using Common.XO.ProtoBuf;
using Common.XO.Requests;
using Common.XO.Requests.DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinkRequest = XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.Internal.ErrorManager
{
    internal sealed class DALErrorManager : IErrorManager
    {
        private IDALStateController context;

        private ILoggingServiceClient LoggingClient => context.LoggingClient;

        public DALErrorManager(IDALStateController controller) => context = controller;

        public async ValueTask<bool> ErrorSendingMessage(object message)
        {
            if (message is BrokerMessage brokerMessage)
            {
                try
                {
                    if (brokerMessage.Header.CommIdentifiers.Count == 1 ||
                        (brokerMessage.Header.CommIdentifiers.Count == 2 &&
                        brokerMessage.Header.CommIdentifiers[1].Service == ServiceType.Servicer))
                    {
                        // A message we sent to the Servicer failed.
                        // Step 1: Is it a Payment?  (Yes => Do something, No => Ignore and Log)
                        LinkRequest request = JsonConvert.DeserializeObject<LinkRequest>(brokerMessage.StringData);
                        LinkAction? actionType = GetActionType(request);
                        if (actionType == LinkAction.Payment)
                        {
                            // Message to Monitor: Connection Failure
                            LinkRequest eventToPublish = CreateNetworkErrorRequestForMonitor(request, "Network Connection Failure");
                            string jsonToPublish = JsonConvert.SerializeObject(eventToPublish);
                            _ = context.Connector.PublishAsync(jsonToPublish, brokerMessage.Header, ServiceType.Monitor, context.Connector.GetServiceIdentifier());

                            // Message on Device: Communication Failure
                            IPaymentDevice device = FindTargetDevice(request.Actions.First().DALRequest.DeviceIdentifier);
                            if (device is ICardDevice cardDevice)
                            {
                                _ = LoggingClient.LogInfoAsync($"DAL: Failed Send Message displayed on device '{device?.DeviceInformation?.SerialNumber ?? "Serial No. Not Available"}'.");
                                LinkRequest networkErrorRequest = CreateNetworkErrorRequestForDevice(request);
                                cardDevice.DeviceUI(networkErrorRequest, CancellationToken.None);
                                await Task.Delay(4000);
                                cardDevice.DeviceSetIdle();
                            }
                            else
                            {
                                _ = LoggingClient.LogInfoAsync($"DAL: Not sending Failed Send Message to device '{device?.DeviceInformation?.SerialNumber ?? "Serial No. Not Available"}'.");
                            }
                        }
                        else
                        {
                            _ = LoggingClient.LogWarnAsync($"DAL: Ignoring Failed Send Message failed with action type '{actionType?.ToString() ?? "<NULL>"}'.");
                        }
                    }
                    else
                    {
                        _ = LoggingClient.LogInfoAsync($"Error sending BrokerMessage, ignoring {JsonConvert.SerializeObject(brokerMessage.Header)}");
                    }
                }
                catch (Exception ex)
                {
                    _ = LoggingClient.LogErrorAsync($"DAL: Handling Failed Send Message failed.", ex);
                    return false;
                }
            }
            else
            {
                _ = LoggingClient.LogWarnAsync($"DAL: Unable to handle Failed Send Message failed with object type '{message.GetType()}'.");
                return false;
            }
            return true;
        }

        private LinkAction? GetActionType(LinkRequest request) => request.Actions?.FirstOrDefault()?.Action;

        private LinkRequest CreateNetworkErrorRequestForMonitor(LinkRequest request, string message)
        {
            LinkRequest linkRequest = JsonConvert.DeserializeObject<LinkRequest>(JsonConvert.SerializeObject(request));
            LinkActionRequest firstAction = linkRequest.Actions.First();
            linkRequest.Actions = new List<LinkActionRequest>()
            {
                new LinkActionRequest()
                {
                    MessageID = firstAction.MessageID,
                    SessionID = firstAction.SessionID,
                    Action = LinkAction.PaymentUpdate,
                    DALRequest = firstAction.DALRequest,
                    DALActionRequest = new LinkDALActionRequest()
                    {
                        DALAction = firstAction.DALActionRequest?.DALAction ?? LinkDALActionType.MonitorMessageUpdate,
                        DeviceUIRequest = new LinkDeviceUIRequest()
                        {
                            UIAction = LinkDeviceUIActionType.Display,
                            Complete = true,
                            MonitorTopMost = linkRequest.Actions[0].DALActionRequest?.DeviceUIRequest?.MonitorTopMost ?? true,
                            MonitorTopMostInterval = linkRequest.Actions[0].DALActionRequest?.DeviceUIRequest?.MonitorTopMostInterval ?? "10",
                            DisplayText = new List<string>()
                            {
                                message ,
                                "Close"
                            },
                        }
                    },
                    PaymentRequest = firstAction.PaymentRequest,
                    PaymentUpdateRequest = new XO.Requests.Payment.LinkPaymentUpdateRequest() { ManualPayment = false }
                }
            };

            return linkRequest;
        }

        private LinkRequest CreateNetworkErrorRequestForDevice(LinkRequest linkRequest)
            => new LinkRequest()
            {
                MessageID = linkRequest.MessageID,
                TCCustID = linkRequest.TCCustID,
                TCPassword = linkRequest.TCPassword,
                IPALicenseKey = linkRequest.IPALicenseKey,
                Actions = new List<LinkActionRequest>()
                    {
                        new LinkActionRequest()
                        {
                            Action = LinkAction.Payment,
                            DALRequest = linkRequest.Actions[0].DALRequest,
                            DALActionRequest = new LinkDALActionRequest()
                            {
                                DALAction = LinkDALActionType.DeviceUI,
                                DeviceUIRequest = new LinkDeviceUIRequest()
                                {
                                    UIAction = LinkDeviceUIActionType.Display,
                                    DisplayText = new List<string>()
                                    {
                                        $"NETWORK CONNECTION\n{new string(' ', 17)}FAILURE"  // 17 spaces makes it look centered on a P200Plus
                                    },
                                }
                            },
                        }
                    }
            };

        public IPaymentDevice FindTargetDevice(LinkDeviceIdentifier deviceIdentifier)
        {
            if (context.TargetDevice != null)
            {
                return context.TargetDevice;
            }

            if (deviceIdentifier == null)
            {
                return null;
            }

            return FindMatchingDevice(deviceIdentifier);
        }

        private IPaymentDevice FindMatchingDevice(LinkDeviceIdentifier deviceIdentifier)
        {
            if (context.TargetDevices is { } && deviceIdentifier is { })
            {
                return context.TargetDevices.Where(device => (device.DeviceInformation != null
                           && device.DeviceInformation.Manufacturer.Equals(deviceIdentifier.Manufacturer, StringComparison.CurrentCultureIgnoreCase)
                            && device.DeviceInformation.Model.Equals(deviceIdentifier.Model, StringComparison.CurrentCultureIgnoreCase)
                            && device.DeviceInformation.SerialNumber.Equals(deviceIdentifier.SerialNumber, StringComparison.CurrentCultureIgnoreCase))).FirstOrDefault();
            }

            return null;
        }
    }
}
