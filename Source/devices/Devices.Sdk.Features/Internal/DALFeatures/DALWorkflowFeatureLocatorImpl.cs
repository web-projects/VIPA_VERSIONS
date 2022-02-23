using Common.XO.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using static Devices.Common.SupportedFeatures;
using static Common.XO.Requests.DAL.LinkDALActionType;

namespace Devices.Sdk.Features.Internal.DALFeatures
{
    internal class DALWorkflowFeatureLocatorImpl : IDeviceWorkflowFeatureLocator
    {
        public IDeviceWorkflowFeature Locate(IEnumerable<IDeviceFeature> features, LinkRequest request)
        {
            static string LinkToFeatureName(ref LinkRequest linkRequest)
            {
                if (linkRequest is null)
                {
                    throw new ArgumentNullException(nameof(linkRequest));
                }

                string targetFeature;
                switch (linkRequest.Actions?.FirstOrDefault()?.DALActionRequest?.DALAction)
                {
                    /**
                     * Payment Workflow Feature
                     */
                    case GetStatus:
                    case GetPayment:
                    case StartManualPayment:
                    case GetVerifyAmount:
                    case GetCreditOrDebit:
                    case GetZIP:
                    case GetPIN:
                    case RemoveCard:
                    case DeviceUI:
                    targetFeature = PaymentWorkflowFeature;
                    break;
                    /**
                        * Signature Workflow Feature
                        */

                    case GetSignature:
                    case EndSignatureMode:
                    targetFeature = SignatureWorkflowFeature;
                    break;
                    /**
                     * ADA Workflow Feature
                     */
                    case StartADAMode:
                    case EndADAMode:
                    targetFeature = ADAWorkflowFeature;
                    break;

                    /*
                     * PreSwipe Workflow Feature
                     */
                    case StartPreSwipeMode:
                    case EndPreSwipeMode:
                    case WaitForCardPreSwipeMode:
                    case PurgeHeldCardData:
                    targetFeature = PreSwipeFeature;
                    break;

                    /**
                     * We always default to the payment workflow feature.
                     */
                    default:
                    targetFeature = PaymentWorkflowFeature;
                    break;
                }

                return targetFeature;
            }

            /**
             * Jon:
             *  This is your jump point for both the core and extended workflow feature sets.
             *  You can add overloads here if needed but by default it's going to always
             *  pick the "Payment" workflow feature which is considered a "core" feature.
             *  
             *  When we're ready to implement ADA then you simply return the workflow feature
             *  for ADA after you've interpreted the link request.
             */

            string featureName = LinkToFeatureName(ref request);

            foreach (IDeviceFeature feature in features)
            {
                if (feature is IDeviceWorkflowFeature)
                {
                    IDeviceWorkflowFeature workflowFeature = feature as IDeviceWorkflowFeature;
                    if (string.Equals(feature.Name, featureName, StringComparison.Ordinal))
                    {
                        return workflowFeature;
                    }
                }
            }

            return null;
        }
    }
}
