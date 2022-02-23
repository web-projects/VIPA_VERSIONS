using Devices.Sdk.Features.Internal.State;
using Devices.Sdk.Features.State.Providers;

namespace Devices.Sdk.Features.Internal.PreProcessor
{
    internal class DALPreProcessorProvider : IDALPreProcessorProvider
    {
        public IDALPreProcessor GetPreProcessor(IDALStateController controller) => new DALPreProcessorImpl(controller);
    }
}
