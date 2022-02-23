using Devices.Sdk.Features.State;
using Devices.Sdk.Features.State.Actions;
using Devices.Sdk.Features.State.Management;

namespace Devices.Sdk.Features
{
    internal interface IDeviceWorkflowFeatureStateActionController
    {
        void AcceptSubWorkflowManager(IDALSubStateManager manager);
        void Recycle();
        IDALSubStateAction GetNextAction(IDeviceWorkflowFeature feature, IDALSubStateAction stateAction);
        IDALSubStateAction GetNextAction(IDeviceWorkflowFeature feature, DALSubWorkflowState state);
    }
}
