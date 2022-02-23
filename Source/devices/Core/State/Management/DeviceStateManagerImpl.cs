using Common.Config;
using Common.Core.Patterns.Queuing;
using Common.Execution;
using Common.XO.Device;
using Common.XO.Requests;
using Common.XO.Requests.DAL;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.Providers;
using Devices.Core.SerialPort.Interfaces;
using Devices.Core.State.Actions;
using Devices.Core.State.Actions.Controllers;
using Devices.Core.State.Actions.Preprocessing;
using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using Devices.Core.State.Providers;
using Devices.Core.State.SubWorkflows;
using Devices.Core.State.SubWorkflows.Management;
using Devices.Sdk;
using Execution;
using Newtonsoft.Json;
using Ninject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Core.State.Management
{
    internal class DeviceStateManagerImpl : IInitializable, IDeviceStateController, IDeviceStateManager, ISubWorkflowHook
    {
        private bool disposed;
        private AutoResetEvent messageReceived = new AutoResetEvent(false);

        //[Inject]
        //public IListenerConnectorProvider ListenerConnectorProvider { get; set; }

        [Inject]
        public IDeviceConfigurationProvider DeviceConfigurationProvider { get; set; }

        [Inject]
        public IDevicePluginLoader DevicePluginLoader { get; set; }

        //[Inject]
        //public ILoggingServiceClientProvider LoggingServiceClientProvider { get; set; }

        [Inject]
        public IDeviceStateActionControllerProvider DeviceStateActionControllerProvider { get; set; }

        [Inject]
        public IControllerVisitorProvider ControllerVisitorProvider { get; set; }

        [Inject]
        public ISubStateManagerProvider SubStateManagerProvider { get; set; }

        [Inject]
        public ISerialPortMonitor SerialPortMonitor { get; set; }

        [Inject]
        public IDeviceCancellationBrokerProvider DeviceCancellationBrokerProvider { get; set; }

        public DeviceSection Configuration { get; private set; }

        //public ILoggingServiceClient LoggingClient { get; private set; }

        //public IListenerConnector Connector { get; private set; }

        public List<ICardDevice> AvailableCardDevices { get; private set; } = new List<ICardDevice>();

        public AppExecConfig AppExecConfig { get; private set; }

        public string PluginPath { get; private set; }

        public List<ICardDevice> TargetDevices { get; private set; }

        public DeviceEventHandler DeviceEventReceived { get; set; }

        public ComPortEventHandler ComPortEventReceived { get; set; }

        public PriorityQueue<PriorityQueueDeviceEvents> PriorityQueue { get; set; }

        public QueueEventHandler QueueEventReceived { get; set; }

        public StateActionRules StateActionRules { get; private set; }

        public bool NeedsDeviceRecovery { get; set; }

        public bool DeviceListenerIsOnline { get; set; }

        private static bool subscribed { get; set; }
        private bool deviceNeedsToBeAdded;

        private IDeviceSubStateController subStateController;
        private IDeviceStateAction currentStateAction;
        private IDeviceStateActionController stateActionController;
        private readonly Stack<object> savedStackState = new Stack<object>();

        public event OnStateChange StateChange;
        public event OnWorkflowStopped WorkflowStopped;
        public event OnRequestReceived RequestReceived;

        private ProgressBar DeviceProgressBar = null;

        public DeviceStateManagerImpl()
        {
            PriorityQueue = new PriorityQueue<PriorityQueueDeviceEvents>();
        }

        public void Initialize()
        {
            DeviceEventReceived = OnDeviceEventReceived;
            ComPortEventReceived = OnComPortEventReceivedAsync;

            SerialPortMonitor.ComportEventOccured += OnComPortEventReceivedAsync;
            SerialPortMonitor.StartMonitoring();

            QueueEventReceived = OnQueueEventOcurred;

            StateActionRules = new StateActionRules();

            DeviceConfigurationProvider.InitializeConfiguration();
            Configuration = DeviceConfigurationProvider.GetAppConfig();
            //LoggingClient = LoggingServiceClientProvider.GetLoggingServiceClient();
            //Connector = ListenerConnectorProvider.GetConnector(DeviceConfigurationProvider.GetConfiguration());

            stateActionController = DeviceStateActionControllerProvider.GetStateActionController(this);

            InitializeConnectorEvents();
        }

        public bool ProgressBarIsActive()
        {
            return (DeviceProgressBar != null);
        }

        public void StartProgressReporting()
        {
            StartProgressBar();
        }

        public void SetAppConfig(AppExecConfig appConfig) => (AppExecConfig) = (appConfig);

        public void SetPluginPath(string pluginPath) => (PluginPath) = (pluginPath);

        public void SetTargetDevices(List<ICardDevice> targetDevices)
        {
            if (TargetDevices != null)
            {
                foreach (var device in TargetDevices)
                {
                    device?.Disconnect();
                }
            }
            TargetDevices = targetDevices;

            if (targetDevices != null)
            {
                bool displayOutput = true;
                foreach (ICardDevice device in targetDevices)
                {
                    device.SetDeviceSectionConfig(Configuration, AppExecConfig, displayOutput);
                    displayOutput = false;
                }
            }
        }

        public void SetPublishEventHandlerAsTask()
        {
            if (TargetDevices != null)
            {
                foreach (var device in TargetDevices)
                {
                    NeedsDeviceRecovery = false;
                    //device.PublishEvent += PublishEventHandlerAsTask;
                    device.DeviceEventOccured += OnDeviceEventReceived;
                }
            }
        }

        public void SaveState(object stateObject) => savedStackState.Push(stateObject);

        public async Task Recovery(IDeviceStateAction state, object stateObject)
        {
            SaveState(stateObject);
            await AdvanceActionWithState(state);
        }

        public void Hook(IDeviceSubStateController controller) => subStateController = (controller);

        public void UnHook() => subStateController = (null);

        public IControllerVisitorProvider GetCurrentVisitorProvider() => ControllerVisitorProvider;

        public IDeviceCancellationBroker GetCancellationBroker() => DeviceCancellationBrokerProvider.GetDeviceCancellationBroker();

        public ISubStateManagerProvider GetSubStateManagerProvider() => SubStateManagerProvider;

        protected void RaiseOnRequestReceived(string data)
        {
            try
            {
                LogMessage("Request received from", data);
                LinkRequest linkRequest = JsonConvert.DeserializeObject<LinkRequest>(data.ToString());
                RequestReceived?.Invoke(linkRequest);
            }
            catch (Exception e)
            {
                LogError(e.Message, data);
                //if (Connector != null)
                {
                    //Connector.Publish(LinkRequestResponseError(null, "DALError", e.Message), new TopicOption[] { TopicOption.Servicer }).ConfigureAwait(false);
                    Console.WriteLine($"RaisedOnRequestReceived: exception='{e.Message}'");
                }
            }
        }

        protected void RaiseOnWorkflowStopped(DeviceWorkflowStopReason reason) => WorkflowStopped?.Invoke(reason);

        private object OnDeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation)
        {
            if (currentStateAction.WorkflowStateType == DeviceWorkflowState.SubWorkflowIdleState)
            {
                if (subStateController != null)
                {
                    IDeviceSubStateManager subStateManager = subStateController as IDeviceSubStateManager;
                    return subStateManager.DeviceEventReceived(deviceEvent, deviceInformation);
                }
            }

            return false;
        }

        private bool DisconnectAllDevices(PortEventType comPortEvent, string portNumber)
        {
            bool peformDeviceDiscovery = false;

            if (TargetDevices != null)
            {
                // dispose of all existing connections so that device recovery re-validates them
                ICardDevice deviceDisconnected = TargetDevices
                    .FirstOrDefault(a => a is ICardDevice && a.DeviceInformation.ComPort.Equals(portNumber, StringComparison.OrdinalIgnoreCase));

                if (deviceDisconnected != null || comPortEvent == PortEventType.Insertion)
                {
                    deviceDisconnected?.Disconnect();
                    peformDeviceDiscovery = true;
                    foreach (var device in TargetDevices)
                    {
                        if (comPortEvent == PortEventType.Removal)
                        {
                            if (device == deviceDisconnected)
                            {
                                Console.WriteLine($"Comport unplugged: '{portNumber}', " +
                                    $"DeviceType '{device.ManufacturerConfigID}', SerialNumber '{device.DeviceInformation?.SerialNumber}'");

                                PublishDeviceDisconnectEvent(device, portNumber);
                            }
                        }
                        device.Dispose();
                    }
                    TargetDevices.Clear();
                }
            }
            //else if (TargetDevice != null)
            //{
            //    // this could be any USB device removal: so we need to make sure it's one of our devices before proceeding.
            //    if (comPortEvent == PortEventType.Removal && !MatchedDevice(TargetDevice, portNumber))
            //    {
            //        return false;
            //    }

            //    TargetDevice.Disconnect();
            //    peformDeviceDiscovery = true;

            //    if (comPortEvent == PortEventType.Removal)
            //    {
            //        _ = LoggingClient.LogInfoAsync($"Comport unplugged. ComportNumber '{portNumber}', " +
            //        $"DeviceType '{TargetDevice?.ManufacturerConfigID}', SerialNumber '{TargetDevice?.DeviceInformation?.SerialNumber}'");

            //        PublishDeviceDisconnectEvent(TargetDevice, portNumber);
            //    }

            //    TargetDevice?.Dispose();
            //}
            return peformDeviceDiscovery;
        }

        private async Task OnComPortEventReceivedAsync(PortEventType comPortEvent, string portNumber)
        {
            Debug.WriteLine($"Comport event received={comPortEvent.ToString()}");

            bool peformDeviceDiscovery = false;

            if (comPortEvent == PortEventType.Insertion)
            {
                Debug.WriteLine($"Comport Plugged. ComportNumber '{portNumber}'. Detecting a new connection...");
                Console.WriteLine($"Comport Plugged. ComportNumber '{portNumber}'. Detecting a new connection...");
                peformDeviceDiscovery = DisconnectAllDevices(comPortEvent, portNumber);
            }
            else if (comPortEvent == PortEventType.Removal)
            {
                peformDeviceDiscovery = DisconnectAllDevices(comPortEvent, portNumber);
            }
            else
            {
                Console.WriteLine($"Comport Event '{comPortEvent}' is not implemented ");
            }

            // only perform discovery when an existing device is disconnected or a new connection is detected
            if (peformDeviceDiscovery)
            {
                if (currentStateAction.WorkflowStateType == DeviceWorkflowState.Manage ||
                    currentStateAction.WorkflowStateType == DeviceWorkflowState.ProcessRequest)
                {
                    Debug.WriteLine($"Device discovery in progress...");
                    Console.WriteLine($"Device discovery in progress...");

                    // wait for USB driver to detach/reattach device
                    await Task.Delay(Configuration.DeviceDiscoveryDelay * 1024);

                    currentStateAction.DoDeviceDiscovery();
                }
                else
                {
                    StateActionRules.NeedsDeviceRecovery = true;

                    if (subStateController != null)
                    {
                        IDeviceSubStateManager subStateManager = subStateController as IDeviceSubStateManager;
                        _ = subStateManager.ComportEventReceived(comPortEvent, portNumber);
                    }
                }
            }
        }

        private void OnQueueEventOcurred()
        {
            //TODO: EventChecker will handle PriorityQueue event dequeuing
            PriorityQueueDeviceEvents queueRequest = PriorityQueue.Dequeue();
        }

        private void LogError(string message, object data)
        {
            string messageId = "Unknown";
            try
            {
                //var request = JsonConvert.DeserializeObject<LinkRequest>(data as string);
                //if (!string.IsNullOrWhiteSpace(request?.MessageID))
                //{
                //    messageId = request.MessageID;
                //}
            }
            finally
            {
                //_ = LoggingClient.LogInfoAsync($"Error from MessageId: '{messageId}':{message}").ConfigureAwait(false);
                Console.WriteLine($"Error from MessageId: '{messageId}':{message}");
            }
        }

        //private LinkRequest LinkRequestResponseError(LinkRequest linkRequest, string codeType, string description)
        //{
        //    if (linkRequest == null)
        //    {
        //        linkRequest = new LinkRequest()
        //        {
        //            LinkObjects = new LinkRequestIPA5Object() { LinkActionResponseList = new List<LinkActionResponse>() },
        //            Actions = new List<LinkActionRequest>() { new LinkActionRequest() { MessageID = "Unknown" } }
        //        };
        //    }
        //    linkRequest.LinkObjects.LinkActionResponseList.Add(new LinkActionResponse()
        //    {
        //        MessageID = linkRequest.Actions[0].MessageID,
        //        Errors = new List<LinkErrorValue>() { new LinkErrorValue() { Code = codeType, Type = codeType, Description = description } }
        //    });
        //    return linkRequest;
        //}

        private void LogMessage(string sendReceive, object data)
        {
            var request = JsonConvert.DeserializeObject<LinkRequest>(data as string);
            string messageId = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(request?.MessageID))
                {
                    messageId = request.MessageID;
                }
            }
            finally
            {
                //_ = LoggingClient.LogInfoAsync($"Request of MessageID: '{messageId}' {sendReceive} Listener.").ConfigureAwait(false);
                Debug.WriteLine($"Request of MessageID: '{messageId}' {sendReceive} Listener.");
            }
        }

        private void InitializeConnectorEvents()
        {
            //    Connector.MessageReceived += ListenerConnector_MessageReceived;
            //    Connector.OfflineConnectivity += ListenerConnector_OfflineConnectivity;
            //    Connector.OnlineConnectivity += ListenerConnector_OnlineConnectivity;
            //    Connector.ChannelClient.ChannelConnected += ChannelClient_ChannelConnected;
            //    Connector.ChannelClient.ChannelDisconnected += ChannelClient_ChannelDisconnected;
            //    Connector.ChannelClient.ChannelReconnected += ChannelClient_ChannelReconnected;
        }

        private void DestroyComportMonitoring()
        {
            if (SerialPortMonitor != null)
            {
                SerialPortMonitor.ComportEventOccured -= OnComPortEventReceivedAsync;
                SerialPortMonitor.StopMonitoring();
            }
        }

        private void DestroyConnectorEvents()
        {
            //    if (Connector != null)
            //    {
            //        Connector.MessageReceived -= ListenerConnector_MessageReceived;
            //        Connector.OfflineConnectivity -= ListenerConnector_OfflineConnectivity;
            //        Connector.OnlineConnectivity -= ListenerConnector_OnlineConnectivity;

            //        if (Connector.ChannelClient != null)
            //        {
            //            Connector.ChannelClient.ChannelConnected -= ChannelClient_ChannelConnected;
            //            Connector.ChannelClient.ChannelDisconnected -= ChannelClient_ChannelDisconnected;
            //            Connector.ChannelClient.ChannelReconnected -= ChannelClient_ChannelReconnected;
            //        }
            //    }
        }

        private void DisconnectFromListener()
        {
            //    Connector.Unsubscribe().Wait(5000);
            //    Connector.Dispose();
            //    Connector = null;
        }

        //private void ChannelClient_ChannelReconnected()
        //{
        //    _ = LoggingClient.LogInfoAsync("DAL is currently reconnected to Listener.");
        //}

        //internal void ChannelClient_ChannelDisconnected(Guid channelId)
        //{
        //    subscribed = false;

        //    _ = LoggingClient.LogInfoAsync($"DAL is currently disconnected from Listener with client id {GetShortClientId(channelId)}");
        //}

        //internal void ChannelClient_ChannelConnected(Guid channelId)
        //{
        //    _ = LoggingClient.LogInfoAsync($"DAL is currently connected to the Listener with client id {GetShortClientId(channelId)}.");

        //    if (!subscribed)
        //    {
        //        Connector.Subscribe(new TopicOption[] { TopicOption.DAL });

        //        subscribed = true;

        //        _ = LoggingClient.LogInfoAsync("DAL is subscribed to the Listener.");
        //    }
        //}

        //private void ListenerConnector_OnlineConnectivity()
        //{
        //    DeviceListenerIsOnline = true;
        //    _ = LoggingClient.LogInfoAsync("Network connectivity is online.");
        //}

        //private void ListenerConnector_OfflineConnectivity()
        //{
        //    DeviceListenerIsOnline = false;
        //    _ = LoggingClient.LogInfoAsync("Network connectivity has gone offline.");
        //}

        //private void ListenerConnector_MessageReceived(Listener.Common.Packets.ListenerPacketHeader header, object message)
        //=> RaiseOnRequestReceived(message as string);

        public void SendDeviceCommand(object message)
            => RaiseOnRequestReceived(message as string);

        public void PublishDeviceConnectEvent(ICardDevice device, string portNumber)
        {
            if (device != null)
            {
                StopProgressBar();

                //string message = $"Comport plugged: '{portNumber}', DeviceType: '{device.ManufacturerConfigID}', Model: '{device.DeviceInformation?.Model}', SerialNumber: '{device.DeviceInformation?.SerialNumber}'";
                //LinkDeviceResponse deviceInfo = new LinkDeviceResponse()
                //{
                //    Manufacturer = device.ManufacturerConfigID,
                //    Model = device.DeviceInformation?.Model,
                //    SerialNumber = device.DeviceInformation?.SerialNumber
                //};
                //List<LinkDeviceResponse> linkDeviceResponses = device.GetDeviceResponse(deviceInfo);

                // build the response into the DALRequest, DALActionResponse
                //if (LastGetStatus?.LinkRequest is { })
                //{
                //    PublishEventHandler(LinkEventResponse.EventTypeType.DEVICE, LinkEventResponse.EventCodeType.DEVICE_PLUGGED,
                //        linkDeviceResponses, LastGetStatus, message);
                //}
            }
        }

        internal void PublishDeviceDisconnectEvent(ICardDevice device, string portNumber)
        {
            if (device != null)
            {
                //string message = $"Comport unplugged: '{portNumber}', DeviceType: '{device.ManufacturerConfigID}', Model: '{device.DeviceInformation?.Model}', SerialNumber: '{device.DeviceInformation?.SerialNumber}'";
                //LinkDeviceResponse deviceInfo = new LinkDeviceResponse()
                //{
                //    Manufacturer = device.ManufacturerConfigID,
                //    Model = device.DeviceInformation?.Model,
                //    SerialNumber = device.DeviceInformation?.SerialNumber
                //};
                //List<LinkDeviceResponse> linkDeviceResponses = device.GetDeviceResponse(deviceInfo);

                //// build the response into the DALRequest, DALActionResponse
                //if (LastGetStatus?.LinkRequest is { })
                //{
                //    LinkActionRequest linkActionRequest = LastGetStatus.LinkRequest.Actions?.FirstOrDefault();
                //    if (linkActionRequest != null)
                //    {
                //        linkActionRequest.DALRequest.LinkObjects = new LinkDALRequestIPA5Object
                //        {
                //            DALResponseData = new LinkDALActionResponse
                //            {
                //                Errors = new List<LinkErrorValue>
                //                {
                //                    new LinkErrorValue
                //                    {
                //                        Code = "DALDeviceDisconnect",
                //                        Type = DALStatus.FoundNotReady.ToString(),
                //                        Description = message
                //                    }
                //                }
                //            }
                //        };
                //    }

                //    PublishEventHandler(LinkEventResponse.EventTypeType.DEVICE, LinkEventResponse.EventCodeType.DEVICE_UNPLUGGED,
                //        linkDeviceResponses, LastGetStatus, message);
                //}
            }
        }

        //internal void PublishEventHandler(LinkEventResponse.EventTypeType eventType, LinkEventResponse.EventCodeType eventCode,
        //    List<LinkDeviceResponse> devices, LinkRequest request, string message)
        //{
        //    string sessionId = request.Actions?[0]?.SessionID;
        //    if (!string.IsNullOrWhiteSpace(sessionId))
        //    {
        //        try
        //        {
        //            var eventToPublish = ComposeEvent(sessionId, eventType, eventCode, devices, request, new List<string>() { (message ?? string.Empty) }, online: DALListenerIsOnline);
        //            string jsonToPublish = JsonConvert.SerializeObject(eventToPublish);
        //            Connector.Publish(jsonToPublish, new string[] { TopicOption.Event.ToString() });
        //        }
        //        catch (Exception xcp)
        //        {
        //            LoggingClient.LogErrorAsync(xcp.Message);
        //        }
        //    }
        //}

        //internal void PublishEventHandlerAsTask(LinkEventResponse.EventTypeType eventType, LinkEventResponse.EventCodeType eventCode,
        //    List<LinkDeviceResponse> devices, LinkRequest request, string message)
        //{
        //    _ = Task.Run(() => PublishEventHandler(eventType, eventCode, devices, request, message)).ConfigureAwait(false);
        //}

        //public LinkResponse ComposeEvent(string sessionId, LinkEventResponse.EventTypeType eventType,
        //    LinkEventResponse.EventCodeType eventCode, List<LinkDeviceResponse> devices, LinkRequest request, List<string> eventData,
        //    bool online = false)
        //{
        //    var eventResponse = new LinkResponse()
        //    {
        //        MessageID = request?.MessageID,
        //        Responses = new List<LinkActionResponse>(1)
        //         {
        //              new LinkActionResponse()
        //              {
        //                  MessageID = request.Actions?[0].MessageID,
        //                  DALResponse = new LinkDALResponse()
        //                   {
        //                        Devices = devices,
        //                        DALIdentifier = request?.Actions?[0].DALRequest?.DALIdentifier ?? DalIdentifier.GetDALIdentifier(),
        //                        OnlineStatus = online
        //                   },
        //                    EventResponse = new LinkEventResponse()
        //                    {
        //                         EventCode = eventCode.ToString(),
        //                         EventType = eventType.ToString(),
        //                         EventID = Guid.NewGuid(),
        //                         EventData = eventData == null ? null : eventData.ToArray(),
        //                         OrdinalID = 0
        //                     },
        //                    SessionResponse = new LinkSessionResponse()
        //                    {
        //                        SessionID = sessionId
        //                    }
        //               }
        //         }
        //    };

        //    if (eventResponse.Responses[0].DALResponse == null)
        //    {
        //        eventResponse.Responses[0].DALResponse = new LinkDALResponse();
        //    }
        //    if (eventResponse.Responses[0].DALResponse.DALIdentifier == null)
        //    {
        //        eventResponse.Responses[0].DALResponse.DALIdentifier = request.Actions?[0].LinkObjects?.ActionResponse?.DALResponse?.DALIdentifier;
        //    }
        //    if (eventResponse.Responses[0].DALResponse.Devices?[0] == null)
        //    {
        //        eventResponse.Responses[0].DALResponse.Devices = request.Actions?[0].LinkObjects?.ActionResponse?.DALResponse?.Devices ?? new List<LinkDeviceResponse>() { null };
        //    }

        //    return eventResponse;
        //}

        public void DisplayDeviceStatus()
        {
            if (TargetDevices is null || TargetDevices.Count == 0)
            {
                Console.WriteLine("NO DEVICE FOUND!!!");
            }
            else
            {
                foreach (ICardDevice device in TargetDevices)
                {
                    Console.WriteLine($"DEVICE ON SERIAL PORT : {device.DeviceInformation?.ComPort} - CONNECTION OPEN");
                    Console.WriteLine($"DISCOVERY AND PROBE __: ['{device?.Name}', '{device?.DeviceInformation?.Model}', '{device?.DeviceInformation?.SerialNumber}']\n");
                }
            }
        }

        public void DeviceStatusUpdate()
        {
            if (deviceNeedsToBeAdded)
            {
                deviceNeedsToBeAdded = false;
                Console.WriteLine();
                DisplayDeviceStatus();
            }
        }

        #region --- state machine management ---

        public Task Complete(IDeviceStateAction state) => AdvanceStateActionTransition(state);

        public void SetWorkflow(LinkDeviceActionType action)
        {
            if (TargetDevices != null)
            {
                LinkRequest linkRequest = new LinkRequest()
                {
                    MessageID = RandomGenerator.BuildRandomString(12),
                    Actions = new List<LinkActionRequest>()
                };

                foreach (ICardDevice device in TargetDevices)
                {
                    linkRequest.Actions.Add(new LinkActionRequest()
                    {
                        Action = LinkAction.DALAction,
                        DeviceActionRequest = new LinkDeviceActionRequest()
                        {
                            DeviceAction = action
                        },
                        DeviceRequest = new LinkDeviceRequest()
                        {
                            DeviceIdentifier = new LinkDeviceIdentifier()
                            {
                                Manufacturer = device.DeviceInformation?.Manufacturer,
                                Model = device.DeviceInformation?.Model,
                                SerialNumber = device.DeviceInformation?.SerialNumber
                            }
                        },
                        DALRequest = new LinkDALRequest()
                        {
                            DeviceIdentifier = new LinkDeviceIdentifier()
                            {
                                Manufacturer = device.DeviceInformation?.Manufacturer,
                                Model = device.DeviceInformation?.Model,
                                SerialNumber = device.DeviceInformation?.SerialNumber
                            }
                        }
                    });
                }

                SendDeviceCommand(JsonConvert.SerializeObject(linkRequest));
            }
            else
            {
                Console.WriteLine("NO TARGET DEVICE IDENTIFIED - CHECK USB/SERIAL CONNECTION SETUP");
            }
        }

        public void LaunchWorkflow() => stateActionController.GetNextAction(DeviceWorkflowState.None).DoWork();

        public DeviceWorkflowState GetCurrentWorkflow()
        {
            return currentStateAction?.WorkflowStateType ?? DeviceWorkflowState.None;
        }

        public void StopWorkflow()
        {
            if (!disposed)
            {
                disposed = true;

                //_ = LoggingClient.LogInfoAsync("Currently shutting down DEVICE Workflow...");
                Console.WriteLine("Currently shutting down DEVICE Workflow...");

                currentStateAction?.Dispose();

                DestroyComportMonitoring();
                DestroyConnectorEvents();
                DisconnectFromListener();

                ExecuteFinalState();
            }
        }

        private async Task AdvanceStateActionTransition(IDeviceStateAction oldState)
        {
            IDeviceStateAction newState = stateActionController.GetNextAction(oldState);

            if (savedStackState.Count > 0)
            {
                newState.SetState(savedStackState.Pop());
            }

            oldState.Dispose();

            currentStateAction = newState;

            LogStateChange(oldState.WorkflowStateType, newState.WorkflowStateType);

            await newState.DoWork();
        }

        private async Task AdvanceActionWithState(IDeviceStateAction oldState)
        {
            IDeviceStateAction newState = stateActionController.GetNextAction(oldState);

            if (savedStackState.Count > 0)
            {
                newState.SetState(savedStackState.Pop());
            }

            oldState.Dispose();

            currentStateAction = newState;

            RaiseStateChange(oldState.WorkflowStateType, newState.WorkflowStateType);

            if (StateActionRules.NeedsDeviceRecovery)
            {
                if (currentStateAction.DoDeviceDiscovery())
                {
                    StateActionRules.NeedsDeviceRecovery = false;
                }
            }

            await newState.DoWork();
        }

        protected void RaiseStateChange(DeviceWorkflowState oldState, DeviceWorkflowState newState)
                    => StateChange?.Invoke(oldState, newState);

        private void ExecuteFinalState()
        {
            using IDeviceStateAction lastAction = stateActionController.GetFinalState();
            lastAction.DoWork().Wait(2000);
        }

        private void LogStateChange(DeviceWorkflowState oldState, DeviceWorkflowState newState)
            //=> _ = LoggingClient.LogInfoAsync($"Workflow State change from '{oldState}' to '{newState}' detected.");
            => Debug.WriteLine($"Workflow State change from '{oldState}' to '{newState}' detected.");

        public async Task Error(IDeviceStateAction state)
        {
            if (state.WorkflowStateType == DeviceWorkflowState.None)
            {
                // TODO: Modify this workflow so that it follows the pattern and simply loops back around
                // to the same final state. In this way, we would run through Shutdown once and then simply
                // decide at that point to stop the workflow because we have no more states to advance to.
                StopWorkflow();
                RaiseOnWorkflowStopped(state.StopReason);
            }
            else
            {
                await AdvanceActionWithState(state);
            }
        }

        public void Dispose()
        {
            StopProgressBar();

            StopWorkflow();

            if (messageReceived != null)
            {
                messageReceived.Dispose();
                messageReceived = null;
            }
        }

        private void StartProgressBar()
        {
        }

        private void StopProgressBar()
        {
            if (DeviceProgressBar != null)
            {
                DeviceProgressBar.Dispose();
                DeviceProgressBar = null;
            }
        }

        #endregion --- state machine management ---
    }
}
