using Devices.Sdk.Features.State;

namespace Devices.Sdk.Features
{
    public interface IDeviceWorkflowFeatureStateTransitionHelper
    {
        DALSubWorkflowState GetNextState(DALSubWorkflowState state, bool exception);
    }
}
