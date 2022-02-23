using Devices.Common.Exceptions;
using Devices.Sdk.Features.State;
using Devices.Sdk.Features.State.Actions;
using Devices.Sdk.Features.State.Management;
using System;
using System.Collections.Generic;

namespace Devices.Sdk.Features.Internal.DALFeatures
{
    public sealed class DALWorkflowFeatureStateActionControllerImpl : IDeviceWorkflowFeatureStateActionController, IDeviceWorkflowFeatureBagOfStateContainer
    {
        private readonly IDictionary<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>> workflowMap
            = new Dictionary<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>>(32);

        private WeakReference<IDALSubStateManager> managerRef;
        private IDALSubStateAction currentStateAction;

        public int NumberOfAvailableStates { get; private set; }

        public void Dispose()
        {
            /*                                              (
             *                                               )
             *                                              (
             *                                               )
             * Nothing to dispose of goes brrrrr.... :-) ===*
             */
        }

        public void Recycle() => currentStateAction = null;

        public void Accept(IDictionary<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>> bagOfStates)
        {
            foreach (KeyValuePair<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>> bag in bagOfStates)
            {
                if (workflowMap.ContainsKey(bag.Key))
                {
                    throw new DALFeatureDuplicateKeyException(bag.Key.ToString(), "A duplicate key entry exists for an existing sub-workflow state.");
                }
                workflowMap.Add(bag.Key, bag.Value);
            }
        }

        public void AcceptSubWorkflowManager(IDALSubStateManager manager)
            => managerRef = new WeakReference<IDALSubStateManager>(manager);

        public IDALSubStateAction GetNextAction(IDeviceWorkflowFeature feature, IDALSubStateAction stateAction)
            => GetNextAction(feature, stateAction.WorkflowStateType);

        public IDALSubStateAction GetNextAction(IDeviceWorkflowFeature feature, DALSubWorkflowState state)
        {
            managerRef.TryGetTarget(out IDALSubStateManager manager);

            IDALSubStateController controller = manager as IDALSubStateController;

            if (currentStateAction is null)
            {
                return (currentStateAction = workflowMap[state](controller));
            }

            IDeviceWorkflowFeatureStateTransitionHelper transitionHelper = feature.GetTransitionHelper();
            DALSubWorkflowState proposedState = transitionHelper.GetNextState(state, currentStateAction.LastException != null);

            if (proposedState == currentStateAction.WorkflowStateType)
            {
                return currentStateAction;
            }

            return (currentStateAction = workflowMap[proposedState](controller));
        }
    }
}
