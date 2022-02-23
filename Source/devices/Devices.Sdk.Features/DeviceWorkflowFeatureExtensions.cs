using Devices.Sdk.Features.State;
using Devices.Sdk.Features.State.Actions;

namespace Devices.Sdk.Features
{
    internal static class DeviceWorkflowFeatureExtensions
    {
        internal static IDALSubStateAction GetNextAction(this IDeviceWorkflowFeature feature, IDALSubStateAction oldState)
            => feature.Context.StateActionController.GetNextAction(feature, oldState);

        internal static IDALSubStateAction GetNextAction(this IDeviceWorkflowFeature feature, DALSubWorkflowState workflowState)
            => feature.Context.StateActionController.GetNextAction(feature, workflowState);
    }
}
