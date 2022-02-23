using Common.XO.Requests;
using System.Collections.Generic;

namespace Devices.Sdk.Features
{
    /// <summary>
    /// Provides the mechanism by which a feature is located to handle specific requests
    /// that are to be processed by DAL.
    /// </summary>
    internal interface IDeviceWorkflowFeatureLocator
    {
        /// <summary>
        /// Locates
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        IDeviceWorkflowFeature Locate(IEnumerable<IDeviceFeature> features, LinkRequest request);
    }
}
