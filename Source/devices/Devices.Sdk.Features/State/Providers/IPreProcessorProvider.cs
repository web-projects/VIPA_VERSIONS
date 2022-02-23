using Devices.Sdk.Features.Internal.State;

namespace Devices.Sdk.Features.State.Providers
{
    internal interface IDALPreProcessorProvider
    {
        IDALPreProcessor GetPreProcessor(IDALStateController controller);
    }
}
