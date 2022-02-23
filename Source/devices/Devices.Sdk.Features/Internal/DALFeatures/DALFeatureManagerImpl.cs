using Common.XO.Requests;
using Devices.Common.AppConfig;
using Devices.Common.Exceptions;
using Ninject;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Devices.Sdk.Features.Internal.DALFeatures
{
    internal sealed class DALFeatureManagerImpl : IDeviceFeatureManager, IInitializable
    {
        private readonly string featurePluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Features");
        private readonly ConcurrentDictionary<string, IDeviceFeature> dalFeatures = new ConcurrentDictionary<string, IDeviceFeature>();

        private bool disposed = false;
        //private ILoggingServiceClient loggingServiceClient;

        public int Count { get; private set; }

        [Inject]
        public IDeviceWorkflowFeatureStateActionController StateActionController { get; set; }

        [Inject]
        public IDeviceFeatureLoader FeatureLoader { get; set; }

        //[Inject]
        //public ILoggingServiceClientProvider LoggingServiceClientProvider { get; set; }

        [Inject]
        public IDeviceWorkflowFeatureLocator FeatureLocator { get; set; }

        public void Initialize()
        {
            //loggingServiceClient = LoggingServiceClientProvider.GetLoggingServiceClient();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                CleanupActionController();
                CleanupAllFeatures();

                Count = 0;
            }
        }

        public IDeviceFeature[] DiscoverFeatures(DeviceSection deviceSection)
        {
            IDeviceFeature[] features = FeatureLoader.LoadFeatures(featurePluginDirectory, deviceSection);
            if (features is null)
            {
                DALFeatureLoadException ex = new DALFeatureLoadException(featurePluginDirectory, "Unable to find any core or extended features for DAL.");
                //_ = loggingServiceClient.LogErrorAsync(ex.Message, ex);
                throw ex;
            }

            IDeviceWorkflowFeatureBagOfStateContainer bagOfStateContainer = StateActionController as IDeviceWorkflowFeatureBagOfStateContainer;

            foreach (IDeviceFeature feature in features)
            {
                // The Feature Manager is the context for all features.
                feature.Context = this;

                if (feature is IDeviceWorkflowFeature)
                {
                    IDeviceWorkflowFeature workflowFeature = feature as IDeviceWorkflowFeature;
                    bagOfStateContainer.Accept(workflowFeature.GetAvailableStateActions());
                }

                _ = dalFeatures.TryAdd(feature.Name, feature);
            }

            Count = dalFeatures.Count + bagOfStateContainer.NumberOfAvailableStates;

            return features;
        }

        public IDeviceInterruptFeature GetInterruptFeature(string featureName)
            => FindFeature<IDeviceInterruptFeature>(featureName);

        public IDeviceWorkflowFeature GetWorkflowFeature(string featureName)
            => FindFeature<IDeviceWorkflowFeature>(featureName);

        private T FindFeature<T>(string featureName)
            where T : class, IDeviceFeature
        {
            if (!dalFeatures.ContainsKey(featureName))
            {
                return default;
            }

            return dalFeatures[featureName] as T;
        }

        private void CleanupAllFeatures()
        {
            foreach (KeyValuePair<string, IDeviceFeature> featurePair in dalFeatures)
            {
                featurePair.Value.Dispose();
            }
        }

        private void CleanupActionController()
        {
            IDeviceWorkflowFeatureBagOfStateContainer container = StateActionController as IDeviceWorkflowFeatureBagOfStateContainer;
            if (container != null)
            {
                container.Dispose();
            }
        }

        public IDeviceWorkflowFeature LocateWorkflowFeature(LinkRequest request)
            => FeatureLocator.Locate(dalFeatures.Select(e => e.Value).AsEnumerable(), request);

        public ConcurrentDictionary<string, string[]> GetAvailableFeatures()
        {
            ConcurrentDictionary<string, string[]> features = new ConcurrentDictionary<string, string[]>();
            foreach (var feature in dalFeatures)
            {
                features.TryAdd(feature.Key, feature.Value.SupportedModels);
            }
            return features;
        }
    }
}
