using Devices.Sdk.Features.Internal.ErrorManager;
using Devices.Sdk.Features.Internal.State;

namespace Devices.Sdk.Features.State.Providers
{
    internal interface IErrorManagerProvider
    {
        IErrorManager GetErrorManager(IDALStateController controller);
    }
}
