using Devices.Sdk.Features.Interrupt;
using Common.XO.Requests;
using System.Threading.Tasks;

namespace Devices.Sdk.Features
{
    public delegate void InterruptFunction(LinkRequest request);

    public interface IDeviceInterruptFeature : IDeviceFeature
    {
        Task ExecuteAsync(InterruptFeatureOptions options);
        void Validate(InterruptFeatureOptions options);
        Task ContinueAsync();
        Task InterruptAsync();
        void SetInterruptManager(IInterruptManager manager);
    }
}
