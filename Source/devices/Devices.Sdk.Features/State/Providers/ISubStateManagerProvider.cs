using Devices.Sdk.Features.Internal.State;
using Devices.Sdk.Features.State.Management;

namespace Devices.Sdk.Features.State.Providers
{
    internal interface ISubStateManagerProvider
    {
        IDALSubStateManager GetSubStateManager(IDALStateController controller);
    }
}
