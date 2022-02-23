using Common.XO.Requests;
using Devices.Common.Interfaces;
using Devices.Sdk.Features.State;
using Devices.Sdk.Features.State.Actions;
using System;
using System.Collections.Generic;

namespace Devices.Sdk.Features
{
    public interface IDeviceWorkflowFeature : IDeviceFeature
    {
        /// <summary>
        /// Returns an instance of the transition helper responsible for traversing
        /// the workflow graph of the current feature.
        /// </summary>
        /// <returns></returns>
        IDeviceWorkflowFeatureStateTransitionHelper GetTransitionHelper();

        /// <summary>
        /// Determines what the initial state on our graph should be based on the
        /// current request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        DALSubWorkflowState GetInitialStateAction(LinkRequest request);

        /// <summary>
        /// Returns a map of all the available sub state actions available in this workflow
        /// and also their subworkflow state bindings.
        /// </summary>
        /// <returns></returns>
        Dictionary<DALSubWorkflowState, Func<IDALSubStateController, IDALSubStateAction>> GetAvailableStateActions();

        /// <summary>
        /// Validates whether or not the feature can handle the current request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cardDevices"></param>
        /// <param name="errorReason"></param>
        /// <returns></returns>
        bool Validate(LinkRequest request, IPaymentDevice[] cardDevices, out string errorReason);
    }
}
