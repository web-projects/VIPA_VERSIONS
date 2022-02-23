using System.Threading.Tasks;

namespace Devices.Sdk.Features.Interrupt
{
    public interface IFeatureManagementTransient
    {
        IDeviceInterruptFeature GetInterruptFeature(string featureName);
        Task ExecuteFeature(IDeviceInterruptFeature feature, InterruptFeatureOptions options);
    }
}
