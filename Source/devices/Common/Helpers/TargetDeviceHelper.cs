using Common.XO.Device;
using Devices.Common.Interfaces;
using System;

namespace Devices.Common.Helpers
{
    public static class TargetDeviceHelper
    {
        public static IPaymentDevice FindTargetDevice(LinkDeviceIdentifier deviceIdentifier, params IPaymentDevice[] cardDevices)
        {
            if (cardDevices?.Length == 1)
            {
                return cardDevices[0];
            }

            if (deviceIdentifier is null)
            {
                return null;
            }

            return FindMatchingDevice(deviceIdentifier, cardDevices);
        }

        private static IPaymentDevice FindMatchingDevice(LinkDeviceIdentifier deviceIdentifier, params IPaymentDevice[] cardDevices)
        {
            IPaymentDevice cardDevice = null;

            foreach (var device in cardDevices)
            {
                if (device.DeviceInformation != null)
                {
                    if (device.DeviceInformation.Manufacturer.Equals(deviceIdentifier.Manufacturer, StringComparison.CurrentCultureIgnoreCase) &&
                        device.DeviceInformation.Model.Equals(deviceIdentifier.Model, StringComparison.CurrentCultureIgnoreCase) &&
                        device.DeviceInformation.SerialNumber.Equals(deviceIdentifier.SerialNumber, StringComparison.CurrentCultureIgnoreCase))
                    {
                        cardDevice = device;
                        break;
                    }
                }
            }

            return cardDevice;
        }
    }

}
