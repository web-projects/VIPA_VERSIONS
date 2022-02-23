using Devices.Sdk.Features.Internal.DALFeatures;
using Devices.Sdk.Features.Internal.ErrorManager;
using Devices.Sdk.Features.Internal.PreProcessor;
using Devices.Sdk.Features.Interrupt;
using Devices.Sdk.Features.State.Providers;
using Ninject.Modules;

namespace Devices.Sdk.Features.Modules
{
    public class DALSdkFeaturesModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDeviceWorkflowFeatureLocator>().To<DALWorkflowFeatureLocatorImpl>();
            Bind<IDeviceWorkflowFeatureStateActionController>().To<DALWorkflowFeatureStateActionControllerImpl>();
            Bind<IDeviceFeatureLoader>().To<DALFeatureLoaderImpl>();
            Bind<IDeviceFeatureManager>().To<DALFeatureManagerImpl>();
            Bind<IInterruptManager>().To<InterruptManagerImpl>();
            Bind<IInterruptFeatureValidator>().To<InterruptFeatureValidatorImpl>();
            Bind<IDALPreProcessorProvider>().To<DALPreProcessorProvider>();
            Bind<IErrorManagerProvider>().To<DALErrorManagerProvider>();
        }
    }
}
