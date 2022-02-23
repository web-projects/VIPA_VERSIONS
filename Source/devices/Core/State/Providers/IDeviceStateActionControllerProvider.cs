using Devices.Core.State.Actions.Controllers;
using Devices.Core.State.Management;

namespace Devices.Core.State.Providers
{
    interface IDeviceStateActionControllerProvider
    {
        IDeviceStateActionController GetStateActionController(IDeviceStateManager manager);
    }
}
