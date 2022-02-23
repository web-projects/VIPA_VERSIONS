using Devices.Core.State.SubWorkflows.Actions.Controllers;
using Devices.Core.State.SubWorkflows.Management;
using System;

namespace Devices.Core.State.SubWorkflows.Providers
{
    internal class DeviceSubStateActionControllerProvider : IDeviceSubStateActionControllerProvider
    {
        public IDeviceSubStateActionController GetStateActionController(IDeviceSubStateManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }
            return new DeviceStateActionSubControllerImpl(manager);
        }
    }
}
