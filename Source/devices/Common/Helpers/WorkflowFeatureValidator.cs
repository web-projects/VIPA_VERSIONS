using Common.XO.Device;
using Common.XO.Requests;
using Devices.Common.Interfaces;
using System;
using System.Linq;
using static Devices.Common.SupportedDevices;

namespace Devices.Common.Helpers
{
    public static class WorkflowFeatureValidator
    {
        public static bool Validate(string[] SupportedModels, LinkRequest request, IPaymentDevice[] cardDevices)
        {
            LinkDeviceIdentifier deviceIdentifier = request.GetDeviceIdentifier();

            IPaymentDevice cardDevice = TargetDeviceHelper.FindTargetDevice(deviceIdentifier, cardDevices);
            if (cardDevice?.DeviceInformation?.Manufacturer != null)
            {
                string featureCardDevice = cardDevice.DeviceInformation.Model;

                //TODO: revisit as new manufacturers are added (this affects Simulator and NoDevice as well)
                if (cardDevice.DeviceInformation.Manufacturer.IndexOf(VerifoneManufacturerId) >= 0 ||
                    cardDevice.DeviceInformation.Manufacturer.IndexOf(MagTekManufacturerId) >= 0)
                {
                    featureCardDevice = $"{cardDevice.DeviceInformation.Manufacturer}-{cardDevice.DeviceInformation.Model}";
                }

                if (SupportedModels.Select(x => x).Where(y => y.Equals(featureCardDevice, StringComparison.OrdinalIgnoreCase)).Count() == 1)
                {
                    return true;
                }
            }
            return false;
        }
    }

}
