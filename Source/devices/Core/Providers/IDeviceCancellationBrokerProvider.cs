using Devices.Core.Cancellation;

namespace Devices.Core.Providers
{
    internal interface IDeviceCancellationBrokerProvider
    {
        IDeviceCancellationBroker GetDeviceCancellationBroker();
    }
}
