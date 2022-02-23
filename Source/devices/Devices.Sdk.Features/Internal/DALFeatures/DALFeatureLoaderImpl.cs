using Devices.Common;
using Devices.Common.AppConfig;
using Devices.Sdk.Features.Internal.DALFeatures.Payment;
using Ninject;
using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Runtime.Loader;

namespace Devices.Sdk.Features.Internal.DALFeatures
{
    internal sealed class DALFeatureLoaderImpl : IDeviceFeatureLoader, IInitializable
    {
        //private ILoggingServiceClient loggingServiceClient;

        //[Inject]
        //internal ILoggingServiceClientProvider LoggingServiceClientProvider { get; set; }

        public void Initialize()
        {
            //loggingServiceClient = LoggingServiceClientProvider.GetLoggingServiceClient();
        }

        private ContainerConfiguration CreateContainerConfiguration(string featureDirectoryPath)
        {
            ContainerConfiguration container = new ContainerConfiguration();

            if (!Directory.Exists(featureDirectoryPath))
            {
                return container;
            }

            var availableAssemblies = Directory
                .GetFiles(featureDirectoryPath, "*.dll", SearchOption.AllDirectories)
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .ToList();

            return container.WithAssemblies(availableAssemblies);
        }

        public IDeviceFeature[] LoadFeatures(string featureDirectoryPath, DeviceSection deviceSection)
        {
            List<IDeviceFeature> features = new List<IDeviceFeature>
            {
                new DALPaymentWorkflowFeatureImpl()
            };

            try
            {
                using CompositionHost container = CreateContainerConfiguration(featureDirectoryPath).CreateContainer();
                IEnumerable<IDeviceFeature> discoveredFeatures = container.GetExports<IDeviceFeature>();

                discoveredFeatures = RemoveFeaturesExcludedInDeviceSection(discoveredFeatures, deviceSection);

                if (discoveredFeatures != null && discoveredFeatures.Count() > 0)
                {
                    features.AddRange(discoveredFeatures);
                }
            }
            catch (CompositionFailedException ex)
            {
                //_ = loggingServiceClient.LogErrorAsync("Feature discovery failed on composition builder.", ex);
            }
            catch (Exception ex)
            {
                //_ = loggingServiceClient.LogErrorAsync("An error occurred while attempting to compose plugin discovery.", ex);
            }

            return features.ToArray();
        }

        private IEnumerable<IDeviceFeature> RemoveFeaturesExcludedInDeviceSection(IEnumerable<IDeviceFeature> features, DeviceSection deviceSection)
        {
            if (!(deviceSection?.Verifone?.AllowPreSwipeMode ?? false) && features.Where(e => e.Name.Equals(SupportedFeatures.PreSwipeFeature)).Any())
            {
                return features.Where(e => !e.Name.Equals(SupportedFeatures.PreSwipeFeature));
            }
            return features;
        }
    }
}
