using Devices.Core.State.Interfaces;
using Devices.Core.State.SubWorkflows.Management;

namespace Devices.Core.State.Providers
{
    internal class SubStateManagerProviderImpl : ISubStateManagerProvider
    {
        public IDeviceSubStateManager GetSubStateManager(IDeviceStateController controller)
            => new GenericSubStateManagerImpl(controller);
    }
}
