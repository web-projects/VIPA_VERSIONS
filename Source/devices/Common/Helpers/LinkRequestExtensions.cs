using Common.XO.Device;
using Common.XO.Requests;
using System.Linq;

namespace Devices.Common.Helpers
{
    public static class LinkRequestExtensions
    {
        public static LinkDeviceIdentifier GetDeviceIdentifier(this LinkRequest linkRequest)
         => linkRequest.Actions?[0]?.DALRequest?.DeviceIdentifier;

        public static int GetAppropriateTimeoutSeconds(this LinkRequest linkRequest, int defaultTimeout)
        {
            int timeout = new int?[4]
            {
                linkRequest?.Timeout,
                linkRequest?.Actions?[0]?.Timeout,
                linkRequest?.Actions?[0]?.PaymentRequest?.CardWorkflowControls?.CardCaptureTimeout,
                linkRequest?.Actions?[0]?.PaymentRequest?.CardWorkflowControls?.SignatureCaptureTimeout
            }.Min().GetValueOrDefault();
            return (timeout > 0 ? timeout : defaultTimeout);
        }

        public static int GetAppropriateManualEntryTimeoutSeconds(this LinkRequest linkRequest, int defaultTimeout)
        {
            int timeout = new int?[3]
            {
                linkRequest?.Timeout,
                linkRequest?.Actions?[0]?.Timeout,
                linkRequest?.Actions?[0]?.PaymentRequest?.CardWorkflowControls?.ManualCardTimeout
            }.Min().GetValueOrDefault();
            return timeout > 0 ? timeout : defaultTimeout;
        }
    }

}
