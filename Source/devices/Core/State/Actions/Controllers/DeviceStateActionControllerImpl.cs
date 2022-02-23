using Devices.Core.State.Enums;
using Devices.Core.State.Interfaces;
using Devices.Core.State.Management;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Devices.Core.State.Actions.Controllers
{
    internal class DeviceStateActionControllerImpl : IDeviceStateActionController
    {
        private readonly IDeviceStateManager manager;
        private readonly ReadOnlyDictionary<DeviceWorkflowState, Func<IDeviceStateController, IDeviceStateAction>> workflowMap =
            new ReadOnlyDictionary<DeviceWorkflowState, Func<IDeviceStateController, IDeviceStateAction>>(
                new Dictionary<DeviceWorkflowState, Func<IDeviceStateController, IDeviceStateAction>>
                {
                    [DeviceWorkflowState.None] = (IDeviceStateController _) => new DeviceNoneStateAction(_),
                    [DeviceWorkflowState.DeviceRecovery] = (IDeviceStateController _) => new DeviceRecoveryStateAction(_),
                    [DeviceWorkflowState.InitializeDeviceCommunication] = (IDeviceStateController _) => new DeviceInitializeDeviceCommunicationStateAction(_),
                    [DeviceWorkflowState.InitializeDeviceHealth] = (IDeviceStateController _) => new DeviceInitializeDeviceHealthStateAction(_),
                    [DeviceWorkflowState.Manage] = (IDeviceStateController _) => new DeviceManageStateAction(_),
                    [DeviceWorkflowState.ProcessRequest] = (IDeviceStateController _) => new DeviceProcessRequestStateAction(_),
                    [DeviceWorkflowState.SubWorkflowIdleState] = (IDeviceStateController _) => new DeviceSubWorkflowIdleStateAction(_),
                    [DeviceWorkflowState.Shutdown] = (IDeviceStateController _) => new DeviceShutdownStateAction(_)
                }
        );

        private IDeviceStateAction currentStateAction;

        public DeviceStateActionControllerImpl(IDeviceStateManager manager) => (this.manager) = (manager);

        public IDeviceStateAction GetFinalState()
            => workflowMap[DeviceWorkflowState.Shutdown](manager as IDeviceStateController);

        public IDeviceStateAction GetNextAction(IDeviceStateAction stateAction)
            => GetNextAction(stateAction.WorkflowStateType);

        public IDeviceStateAction GetNextAction(DeviceWorkflowState state)
        {
            IDeviceStateController controller = manager as IDeviceStateController;
            if (currentStateAction == null)
            {
                return (currentStateAction = workflowMap[DeviceWorkflowState.None](controller));
            }

            DeviceWorkflowState proposedState = DeviceStateTransitionHelper.GetNextState(state, currentStateAction.LastException != null);

            if (proposedState == currentStateAction.WorkflowStateType)
            {
                return currentStateAction;
            }

            return (currentStateAction = workflowMap[proposedState](controller));
        }

        public IDeviceStateAction GetSpecificAction(DeviceWorkflowState proposedState)
        {
            IDeviceStateController controller = manager as IDeviceStateController;

            return (currentStateAction = workflowMap[proposedState](controller));
        }
    }
}
