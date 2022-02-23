using Common.XO.Requests;
using Devices.Common.AppConfig;
using System;
using System.Collections.Concurrent;

namespace Devices.Sdk.Features
{
    internal interface IDeviceFeatureManager : IDisposable
    {
        /// <summary>
        /// Gets the number of features currently loaded.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets an instance of the state action controller responsible for
        /// creating proper instances of state actions.
        /// </summary>
        IDeviceWorkflowFeatureStateActionController StateActionController { get; }

        /// <summary>
        /// Gets an instance of the feature locator which is responsible for
        /// getting the appropriate workflow feature to handle a request.
        /// </summary>
        IDeviceWorkflowFeatureLocator FeatureLocator { get; }

        /// <summary>
        /// Discovers all new features that are currently available to be loaded.
        /// </summary>
        /// <returns></returns>
        IDeviceFeature[] DiscoverFeatures(DeviceSection deviceSettings);

        /// <summary>
        /// Returns an instance to a specific workflow feature based on its name.
        /// </summary>
        /// <param name="featureName">Name of the workflow feature to retrieve.</param>
        /// <returns></returns>
        IDeviceWorkflowFeature GetWorkflowFeature(string featureName);

        /// <summary>
        /// Returns an instance to a specific interrupt feature based on its name.
        /// </summary>
        /// <param name="featureName">Name of the interrupt feature to retrieve.</param>
        /// <returns></returns>
        IDeviceInterruptFeature GetInterruptFeature(string featureName);

        /// <summary>
        /// Returns an instance to a specific workflow feature than can handle the 
        /// specified link request.
        /// </summary>
        /// <param name="request">
        /// The link request containing information needed to locate an appropriate feature.
        /// </param>
        /// <returns></returns>
        IDeviceWorkflowFeature LocateWorkflowFeature(LinkRequest request);

        /// <summary>
        /// Returns a dictonary of names of features and supported models that have been
        /// loaded and are currently available.
        /// </summary>
        /// <returns></returns>
        ConcurrentDictionary<string, string[]> GetAvailableFeatures();
    }
}
