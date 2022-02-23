﻿using Devices.Common.Config;
using App.Helpers.EMVKernel;

namespace Devices.Common
{
    public class DeviceInformation
    {
        public string ComPort { get; set; }
        public string SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }
        public string ProductIdentification { get; set; }
        public string VendorIdentifier { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public byte ConfigurationHostId { get; set; } = VerifoneSettingsSecurityConfiguration.ConfigurationHostId;
        public byte OnlinePinKeySetId { get; set; } = VerifoneSettingsSecurityConfiguration.OnlinePinKeySetId;
        public byte ADEKeySetId { get; set; } = VerifoneSettingsSecurityConfiguration.ADEKeySetId;
        public string ConfigurationPackageActive { get; set; }
        public string SigningMethodActive { get; set; }
        public string ActiveCustomerId { get; set; }
        public bool EnableHMAC { get; set; } = false;
        public string VipaPackageTag { get; set; }
        public string CertPackageTag { get; set; }
        public string IdleImagePackageTag { get; set; }
        public string ContactlessKernelInformation { get; set; }
        public string EMVL2KernelVersion { get; set; }
        public VOSVersions VOSVersions { get; set; }
    }
}
