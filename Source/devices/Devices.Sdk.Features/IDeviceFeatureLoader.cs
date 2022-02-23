using Devices.Common.AppConfig;

namespace Devices.Sdk.Features
{
    internal interface IDeviceFeatureLoader
    {
        IDeviceFeature[] LoadFeatures(string featureDirectoryPath, DeviceSection deviceSecion);
    }
}
