using Common.Execution;
using Common.XO.Requests;
using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Common.Helpers;
using Devices.Common.Interfaces;
using Devices.Core.Cancellation;
using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using Devices.Core.State.SubWorkflows.Actions;
using Devices.Core.State.SubWorkflows.Actions.Controllers;
using Devices.Core.State.SubWorkflows.Providers;
using Devices.Core.State.Visitors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Devices.Core.State.SubWorkflows.Management
{
    internal class GenericSubStateManagerImpl : IDeviceSubStateManager, IDeviceSubStateController, IStateControllerVisitable<ISubWorkflowHook, IDeviceSubStateController>
    {
        private readonly IDeviceStateController context;
        private readonly Stack<object> savedStackState = new Stack<object>();

        private Timer globalExecutionTimer;
        private WorkflowOptions launchOptions;
        private CancellationTokenSource cancellationTokenSource;
        private readonly InitialStateProvider initialStateProvider = new InitialStateProvider();
        private readonly DeviceSubStateActionControllerProvider subStateActionControllerProvider = new DeviceSubStateActionControllerProvider();

        private IDeviceSubStateAction currentStateAction;
        private IDeviceSubStateActionController stateActionController;

        private ProgressBar DeviceProgressBar = null;

        public DeviceSection Configuration => context.Configuration;

        //public ILoggingServiceClient LoggingClient => context.LoggingClient;

        //public IListenerConnector Connector => context.Connector;

        public List<ICardDevice> TargetDevices => context.TargetDevices;

        public bool DidTimeoutOccur { get; private set; }

        public DeviceEvent DeviceEvent { get; private set; }

        public event OnSubWorkflowCompleted SubWorkflowComplete;
        public event OnSubWorkflowError SubWorkflowError;

        internal GenericSubStateManagerImpl(IDeviceStateController _)
        {
            context = _;
        }

        public void Dispose()
        {
            StopProgressBar();

            TeardownGlobalExecutionTimer();
            TeardownCancellationTokenSource();
        }

        public void Accept(IStateControllerVisitor<ISubWorkflowHook, IDeviceSubStateController> visitor)
            => visitor.Visit(context as ISubWorkflowHook, this);

        public Task Complete(IDeviceSubStateAction state)
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

        public Task Error(IDeviceSubStateAction state)
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
                StartProgressBar();

                if (launchOptions.StateObject != null)
                {
                    SaveState(launchOptions.StateObject);
                }

                if (launchOptions.ExecutionTimeout.HasValue)
                {
                    SetupGlobalExecutionTimer();
                }

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

        public IDeviceCancellationBroker GetDeviceCancellationBroker() => context.GetCancellationBroker();

        public void RequestReceived(LinkRequest request)
        {

        }

        private void ExecuteTransition()
        {
            stateActionController = subStateActionControllerProvider.GetStateActionController(this);
            DeviceSubWorkflowState initialState = initialStateProvider.DetermineInitialState(this.launchOptions.StateObject);
            AdvanceStateActionTransition(initialState);
        }

        private bool IsEndOfWorkflow(IDeviceSubStateAction currentState)
        {
            // If this is a cutoff state then there is nothing further for us to do.
            if (currentState.WorkflowCutoff)
            {
                currentState.Dispose();

                return true;
            }
            return false;
        }

        private void AdvanceStateActionTransition(IDeviceSubStateAction oldState)
        {
            currentStateAction = stateActionController.GetNextAction(oldState);
            _ = AssignAndForward(currentStateAction, oldState);
        }

        private void AdvanceStateActionTransition(DeviceSubWorkflowState workflowState)
        {
            currentStateAction = stateActionController.GetNextAction(workflowState);
            _ = AssignAndForward(currentStateAction, null);
        }

        private async Task AssignAndForward(IDeviceSubStateAction newState, IDeviceSubStateAction oldState)
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

            LogStateChange(oldState?.WorkflowStateType ?? DeviceSubWorkflowState.Undefined, newState.WorkflowStateType);

            await newState.DoWork();
        }

        #region Logging Functionality 

        private void LogStateChange(DeviceSubWorkflowState oldState, DeviceSubWorkflowState newState)
        {
            // TODO: Add your logging here on the state change and update the messaging if necessary.
            //LoggingClient.LogInfoAsync($"State change from {oldState} to {newState}.");
            Debug.WriteLine($"State change from {oldState} to {newState}.");
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
            => globalExecutionTimer.Change(launchOptions.ExecutionTimeout.Value, Timeout.Infinite);

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
                catch (AggregateException e)
                {
                    // TODO: Log any aggregate exceptions you get here.
                    //LoggingClient.LogErrorAsync(e.ToString());
                    Console.WriteLine(e.ToString());
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

        protected void RaiseOnWorkflowComplete() => SubWorkflowComplete?.Invoke();

        protected void RaiseOnWorkflowError() => SubWorkflowError?.Invoke();

        public void RequestReceived(object request)
        {

        }

        public object DeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation)
        {
            DeviceEvent = deviceEvent;

            if (currentStateAction?.LaunchRules?.DisableDeviceEventPreProcessing ?? false)
            {
                currentStateAction?.DeviceEventReceived(deviceEvent, deviceInformation);
            }
            else
            {
                // TODO: where to handle events via a Preprocessing - most likely in main controller OnDeviceEventReceived().
                //PriorityEventType eventType = PriorityEventType.Undefined;

                switch (deviceEvent)
                {
                    //case DeviceEvent.DevicePlugged:
                    //case DeviceEvent.DeviceUnplugged:
                    //case DeviceEvent.CancelKeyPressed:
                    //{
                    //   eventType = PriorityEventType.UserCancel;
                    //   break;
                    //}
                    case DeviceEvent.ProgressBarActive:
                    {
                        return (DeviceProgressBar != null);
                    }
                }

                /*if (eventType != PriorityEventType.Undefined)
                {
                    PriorityQueueDeviceEvents deviceQueueItem = new PriorityQueueDeviceEvents(eventType, (double)eventType);
                    context?.PriorityQueue.Enqueue(deviceQueueItem);
                }*/

                //RequestCancellationIfNecessary();
            }

            return false;
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
    }
}
