using Devices.Sdk.Features.Internal.ErrorManager;
using Devices.Sdk.Features.Internal.State;

namespace Devices.Sdk.Features.State.Providers
{
    internal sealed class DALErrorManagerProvider : IErrorManagerProvider
    {
        public IErrorManager GetErrorManager(IDALStateController _)
            => new DALErrorManager(_);
    }
}
