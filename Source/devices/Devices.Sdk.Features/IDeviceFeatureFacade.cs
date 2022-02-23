using Common.XO.Requests;

namespace Devices.Sdk.Features
{
    internal interface IDeviceFeatureFacade
    {
        IDeviceInterruptFeature GetInterruptFeature(string featureName);
        IDeviceWorkflowFeature LocateWorkflowFeature(LinkRequest request);
    }
}
