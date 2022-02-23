using System;

namespace Devices.Sdk.Features
{
    public interface IDeviceFeature : IDisposable
    {
        /// <summary>
        /// Returns an instance of the parent feature manager.
        /// </summary>
        internal IDeviceFeatureManager Context { get; set; }

        /// <summary>
        /// The name of the current feature.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The user-friendly name of the feature.
        /// </summary>
        public string UserFriendlyName { get; }

        /// <summary>
        /// Specifies whether or not this feature is workflow or interrupt in nature.
        /// </summary>
        public DeviceFeatureType FeatureType { get; }

        /// <summary>
        /// Allows the specification of specific device types (manufacturers) that a feature supports.
        /// </summary>
        public string[] SupportedDeviceTypes { get; }

        /// <summary>
        /// Allows the specification of specific device models that a feature supports.
        /// </summary>
        public string[] SupportedModels { get; }
    }
}
