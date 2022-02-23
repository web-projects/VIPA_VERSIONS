using Devices.Core.State.Actions.Controllers;
using Devices.Core.State.Management;
using System;

namespace Devices.Core.State.Providers
{
    class DeviceStateActionControllerProvider : IDeviceStateActionControllerProvider
    {
        public IDeviceStateActionController GetStateActionController(IDeviceStateManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            return new DeviceStateActionControllerImpl(manager);
        }
    }
}
