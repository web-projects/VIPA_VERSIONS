using System;
using System.Collections.Generic;

namespace Devices.Common.Config
{
    [Serializable]
    public class VerifoneSettings
    {
        public int SortOrder { get; set; } = -1;
        public List<string> SupportedDevices { get; internal set; } = new List<string>();
        public byte ConfigurationHostId { get; set; } = VerifoneSettingsSecurityConfiguration.ConfigurationHostId;
        public byte OnlinePinKeySetId { get; set; } = VerifoneSettingsSecurityConfiguration.OnlinePinKeySetId;
        public byte ADEKeySetId { get; set; } = VerifoneSettingsSecurityConfiguration.ADEKeySetId;
        public List<string> ConfigurationPackages { get; internal set; } = new List<string>();
        public string ConfigurationPackageActive { get; set; } = VerifoneSettingsConfigurationPackages.Epic;
        public string ActiveCustomerId { get; set; } = CustomerIdentifers.Default;
        public string Reboot24Hour { get; set; } = "020000";
        public bool AllowPreSwipeMode { get; set; }
    }

    /// <summary>
    /// Verifone Configuration Support
    /// NJT : non-EMV
    /// EPIC: EMV
    /// </summary>
    public static class VerifoneSettingsConfigurationPackages
    {
        public static string Epic = "EPIC";
        public static string NJT = "NJT";
    }

    /// <summary>
    /// Verifone Signing Methods Support
    /// SPHERE  : sphere signed
    /// VERIFONE: development
    /// </summary>
    public static class VerifoneSettingsSigningMethods
    {
        public static string Sphere = "SPHERE";
        public static string Verifone = "VERIFONE";
    }

    /// <summary>
    /// Verifone Signing Methods Support
    /// SPHERE  : sphere signed
    /// VERIFONE: development
    /// </summary>
    public static class CustomerIdentifers
    {
        public static string Default = "199";
        public static string NYU = "250";
    }
}
