using Devices.Core.State.Enums;
using System;
using System.Linq;
using Common.XO.Requests;

namespace Devices.Core.State.SubWorkflows.Providers
{
    internal class InitialStateProvider
    {
        public DeviceSubWorkflowState DetermineInitialState(LinkRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            LinkActionRequest linkActionRequest = request.Actions.First();
            DeviceSubWorkflowState proposedState = ((linkActionRequest.DeviceActionRequest?.DeviceAction) switch
            {
                LinkDeviceActionType.DisplayIdleScreen => DeviceSubWorkflowState.DisplayIdleScreen,
                LinkDeviceActionType.ReportVipaVersions => DeviceSubWorkflowState.ReportVIPAVersions,
                _ => DeviceSubWorkflowState.Undefined
            });

            return proposedState;
        }
    }
}
