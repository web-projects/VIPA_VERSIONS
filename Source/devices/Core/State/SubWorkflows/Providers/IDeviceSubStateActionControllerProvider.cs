using Devices.Core.State.SubWorkflows.Actions.Controllers;
using Devices.Core.State.SubWorkflows.Management;

namespace Devices.Core.State.SubWorkflows.Providers
{
    internal interface IDeviceSubStateActionControllerProvider
    {
        IDeviceSubStateActionController GetStateActionController(IDeviceSubStateManager manager);
    }
}
