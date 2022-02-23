using Devices.Core.Cancellation;

namespace Devices.Core.Providers
{
    internal class DeviceCancellationBrokerProviderImpl : IDeviceCancellationBrokerProvider
    {
        public IDeviceCancellationBroker GetDeviceCancellationBroker() => new DeviceCancellationBrokerImpl();
    }
}
