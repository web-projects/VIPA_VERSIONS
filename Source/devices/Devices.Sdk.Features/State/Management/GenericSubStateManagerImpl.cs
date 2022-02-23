using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Sdk.Features.Cancellation;
using Devices.Sdk.Features.Internal.State;
using Devices.Sdk.Features.Interrupt;
using Devices.Sdk.Features.State.Actions;
using Devices.Sdk.Features.State.Providers;
using Devices.Sdk.Features.State.Visitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LinkRequest = Common.XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.State.Management
{
    internal class GenericSubStateManagerImpl : IDALSubStateManager, IDeviceSubStateController, IStateControllerVisitable<ISubWorkflowHook, IDALSubStateController>
    {
        public DeviceSection Configuration => context.Configuration;
        public ConcurrentDictionary<string, string[]> AvailableFeatures => context.FeatureManager.GetAvailableFeatures();

        //public ILoggingServiceClient LoggingClient => context.LoggingClient;

        //public IBrokerConnector Connector => context.Connector;

        public IDALPreProcessor PreProcessor => context.PreProcessor;

        public IPaymentDevice TargetDevice => context.TargetDevice;

        public List<IPaymentDevice> TargetDevices => context.TargetDevices;

        public bool DidTimeoutOccur { get; private set; }

        public bool DidCancellationOccur { get; private set; }

        public DeviceEvent DeviceEvent { get; private set; }
        public IDeviceHighLevelRegister Register { get; private set; }
        public CommunicationObject LastGetStatus { get => context.LastGetStatus; set => context.LastGetStatus = value; }

        public IInterruptManager InterruptManager => context.InterruptManager;

        private readonly IDALStateController context;
        private readonly Stack<object> savedStackState = new Stack<object>();

        private Timer globalExecutionTimer;
        private WorkflowOptions launchOptions;
        private CancellationTokenSource cancellationTokenSource;

        private IDeviceWorkflowFeature currentWorkflowFeature;
        private IDALSubStateAction currentStateAction;

        public event OnSubWorkflowCompleted SubWorkflowComplete;
        public event OnSubWorkflowError SubWorkflowError;

        public GenericSubStateManagerImpl(IDALStateController contextController)
        {
            context = contextController;
            //context.RequestReceived += RequestReceived;
            Register = context.GetFreshHLRegister();
        }

        public void Dispose()
        {
            //context.RequestReceived -= RequestReceived;
            TeardownGlobalExecutionTimer();
            TeardownCancellationTokenSource();
        }

        public void Accept(IStateControllerVisitor<ISubWorkflowHook, IDALSubStateController> visitor)
            => visitor.Visit(context as ISubWorkflowHook, this);

        public Task Complete(IDALSubStateAction state)
        {
            if (IsEndOfWorkflow(state))
            {
                RaiseOnWorkflowComplete();
            }
            else
            {
                AdvanceStateActionTransition(state);
            }
            return Task.CompletedTask;
        }

        public Task Error(IDALSubStateAction state)
        {
            if (IsEndOfWorkflow(state))
            {
                RaiseOnWorkflowError();
            }
            else
            {
                AdvanceStateActionTransition(state);
            }
            return Task.CompletedTask;
        }

        public void LaunchWorkflow(WorkflowOptions launchOptions)
        {
            DeviceEvent = DeviceEvent.None;

            if (launchOptions != null)
            {
                if (launchOptions.LinkRequest != null)
                {
                    //CommunicationObject commObject = new CommunicationObject(launchOptions.Header, launchOptions.LinkRequest);
                    //SaveState(commObject);
                }

                if (launchOptions.ExecutionTimeout.HasValue)
                {
                    SetupGlobalExecutionTimer();
                }

                currentWorkflowFeature = launchOptions.TargetFeature;
                this.launchOptions = launchOptions;

                // Set global timeout
                if (launchOptions.ExecutionTimeout.HasValue)
                {
                    BeginGlobalExecutionTimer();
                }

                ExecuteTransition();
            }
            else
            {
                // TODO: You need to return back an error and basically report that nothing could be completed.
            }
        }

        public void SaveState(object stateObject) => savedStackState.Push(stateObject);

        public IDeviceCancellationBroker GetDeviceCancellationBroker()
            => context.GetCancellationBroker()
                .SetFailureOutcomeCallback(() => Register.LastAsyncBrokerOutcome = LEBO.Failure)
                .SetSuccessOutcomeCallback(() => Register.LastAsyncBrokerOutcome = LEBO.Success);

        private void ExecuteTransition()
        {
            DALSubWorkflowState initialState = currentWorkflowFeature.GetInitialStateAction(launchOptions.LinkRequest);
            AdvanceStateActionTransition(initialState);
        }

        private bool IsEndOfWorkflow(IDALSubStateAction currentState)
        {
            // If this is a cutoff state then there is nothing further for us to do.
            if (currentState is { } && currentState.WorkflowCutoff)
            {
                currentState.Dispose();

                return true;
            }
            return false;
        }

        private void AdvanceStateActionTransition(IDALSubStateAction oldState)
        {
            currentStateAction = currentWorkflowFeature.GetNextAction(oldState);
            _ = AssignAndForward(currentStateAction, oldState);
        }

        private void AdvanceStateActionTransition(DALSubWorkflowState workflowState)
        {
            currentStateAction = currentWorkflowFeature.GetNextAction(workflowState);
            _ = AssignAndForward(currentStateAction, null);
        }

        private async Task AssignAndForward(IDALSubStateAction newState, IDALSubStateAction oldState)
        {
            if (savedStackState.Count > 0)
            {
                newState.SetState(savedStackState.Pop());
            }

            if (newState.LaunchRules?.RequestCancellationToken ?? false)
            {
                newState.SetCancellationToken(GetCancellationToken());
            }

            oldState?.Dispose();

            LogStateChange(oldState?.WorkflowStateType ?? DALSubWorkflowState.Undefined, newState.WorkflowStateType);

            await newState.DoWork();
        }

        #region Logging Functionality 

        private void LogStateChange(DALSubWorkflowState oldState, DALSubWorkflowState newState)
        {
            // TODO: Add your logging here on the state change and update the messaging if necessary.
            LoggingClient.LogInfoAsync($"State change from {oldState} to {newState}.");
        }

        #endregion

        #region --- Global Execution Timer ---

        private void SetupGlobalExecutionTimer()
        {
            TeardownGlobalExecutionTimer();
            globalExecutionTimer = new Timer(new TimerCallback(GlobalExecutionTimerCallback));
        }

        private void TeardownGlobalExecutionTimer()
        {
            if (globalExecutionTimer != null)
            {
                StopGlobalExecutionTimer();
                globalExecutionTimer.Dispose();
                globalExecutionTimer = null;
            }
        }

        private void BeginGlobalExecutionTimer()
            => globalExecutionTimer.Change(launchOptions.ExecutionTimeout.Value * 1000, Timeout.Infinite);

        private void StopGlobalExecutionTimer()
            => globalExecutionTimer.Change(Timeout.Infinite, Timeout.Infinite);

        private void GlobalExecutionTimerCallback(object state)
        {
            DidTimeoutOccur = true;
            DeviceEvent = DeviceEvent.RequestTimeout;
            RequestCancellationIfNecessary();
        }

        #endregion --- Global Execution Timer ---

        #region --- Cancellation Tokens ---

        private CancellationToken GetCancellationToken()
        {
            if (cancellationTokenSource is null)
            {
                cancellationTokenSource = new CancellationTokenSource();
            }
            return cancellationTokenSource.Token;
        }

        private void RequestCancellationIfNecessary()
        {
            if (cancellationTokenSource != null)
            {
                try
                {
                    cancellationTokenSource.Cancel(true);
                }
                catch (AggregateException ex)
                {
                    // TODO: Log any aggregate exceptions you get here.
                    LoggingClient.LogErrorAsync(ex.Message, ex);
                }
            }
        }

        private void TeardownCancellationTokenSource()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }

        #endregion --- Cancellation Tokens ---

        protected void RaiseOnWorkflowComplete() => SubWorkflowComplete?.Invoke();

        protected void RaiseOnWorkflowError() => SubWorkflowError?.Invoke();

        //public void RequestReceived(CommunicationHeader header, LinkRequest linkRequest)
        //{
        //    //If PreProcessor is disabled...
        //    if ((currentStateAction?.LaunchRules?.DisableRequestPreProcessing ?? false) && currentStateAction.RequestSupported(linkRequest))
        //    {
        //        currentStateAction?.RequestReceived(header, linkRequest);
        //    }
        //    else
        //    {
        //        if (PreProcessor.CanHandleRequest(linkRequest, this))
        //        {
        //            PreProcessor.HandleRequest(header, linkRequest, this);
        //        }
        //        else //If ! handled by PreProcessor
        //        {
        //            currentStateAction?.Controller?.LoggingClient?.LogWarnAsync($"Received Request was ignored {linkRequest.MessageID}");
        //        }
        //    }
        //}

        public void CancelCurrentSubStateAction(DeviceEvent deviceEvent)
        {
            context.LoggingClient.LogInfoAsync("Cancel Current SSA Requested");

            if (cancellationTokenSource != null)
            {
                DidCancellationOccur = true;
                DeviceEvent = deviceEvent;
                RequestCancellationIfNecessary();
            }
            else
            {
                if (currentStateAction is null)
                {
                    context.LoggingClient.LogInfoAsync("Cancel Current SSA is being ignored.");
                    return;
                }
                Complete(currentStateAction);
            }
        }

        public void DeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation)
        {
            DeviceEvent = deviceEvent;

            if (currentStateAction?.LaunchRules?.DisableDeviceEventPreProcessing ?? false)
            {
                currentStateAction?.DeviceEventReceived(deviceEvent, deviceInformation);
            }
            else
            {
                // TODO: where to handle events via a Preprocessing - most likely in main controller OnDeviceEventReceived().
                /*PriorityEventType eventType = PriorityEventType.Undefined;

                switch (deviceEvent)
                {
                    //case DeviceEvent.DevicePlugged:
                    //case DeviceEvent.DeviceUnplugged:
                    case DeviceEvent.CancelKeyPressed:
                    {
                        eventType = PriorityEventType.UserCancel;
                        break;
                    }
                }

                if (eventType != PriorityEventType.Undefined)
                {
                    PriorityQueueDeviceEvents deviceQueueItem = new PriorityQueueDeviceEvents(eventType, (double)eventType);
                    context?.PriorityQueue.Enqueue(deviceQueueItem);
                }*/

                //if (deviceEvent != DeviceEvent.PinBypassKeyPressed)
                //{
                //    RequestCancellationIfNecessary();
                //}
            }
        }

        public async Task ComportEventReceived(PortEventType comPortEvent, string portNumber)
        {
            DeviceEvent = (comPortEvent == PortEventType.Insertion) ? DeviceEvent.DevicePlugged : DeviceEvent.DeviceUnplugged;

            if (currentStateAction?.LaunchRules?.DisableRequestPreProcessing ?? false)
            {
                await currentStateAction?.ComportEventReceived(comPortEvent, portNumber);
            }
            else
            {
                RequestCancellationIfNecessary();
            }
        }

        bool IDALSubStateController.RequestWorkflowCancellation()
        {
            RequestCancellationIfNecessary();
            return (cancellationTokenSource != null);
        }

        //public IPaymentDevice[] GetAvailableDevices() => context.GetAvailableDevices();

        public IDeviceInterruptFeature GetInterruptFeature(string featureName)
            => context.FeatureManager.GetInterruptFeature(featureName);

        public async Task ExecuteFeature(IDeviceInterruptFeature feature, InterruptFeatureOptions options)
        {
            await context.InterruptManager.ExecuteAsync(feature, options);
        }

        public bool RequestSupported(LinkRequest request) =>
            currentStateAction?.RequestSupported(request) ?? false;
    }
}
