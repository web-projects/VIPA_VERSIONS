using Devices.Common;

namespace Devices.Sdk.Features.Interrupt
{
    internal sealed class InterruptFeatureValidatorImpl : IInterruptFeatureValidator
    {
        public bool CanInterruptFeatureHandle(IDeviceInterruptFeature feature, ICardDevice cardDevice)
        {
            if (cardDevice is null || feature is null)
            {
                return false;
            }

            // TODO: validation will be deferred to next PR
            return true;
        }
    }
}
