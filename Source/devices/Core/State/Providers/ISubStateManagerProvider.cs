using Devices.Core.State.SubWorkflows.Management;
using Devices.Core.State.Interfaces;

namespace Devices.Core.State.Providers
{
    internal interface ISubStateManagerProvider
    {
        IDeviceSubStateManager GetSubStateManager(IDeviceStateController controller);
    }
}
