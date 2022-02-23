namespace Devices.Common.Config
{
    /// <summary>
    /// VSS ONLINE PIN SETUP: hostId = 0x02, KeySetId = 0x00
    /// IPP ONLINE PIN SETUP: hostId = 0x05, KeySetId = 0x01
    /// </summary>
    public static class VerifoneSettingsSecurityConfiguration
    {
        // ADE KEY
        public const byte ADEHostIdProd = 0x00;
        public const byte ADEHostIdTest = 0x08;
        public const byte DUKPTEngineVSS = 0x02;
        public const byte DUKPTEngineIPP = 0x05;
        public const byte ConfigurationHostId = DUKPTEngineIPP;
        // DEBIT PIN KEY
        public const byte OnlinePinKeySetIdVSS = 0x00;
        public const byte OnlinePinKeySetIdIPP = 0x01;
        public const byte ADEKeySetId = ADEHostIdProd;
        public const byte OnlinePinKeySetId = OnlinePinKeySetIdIPP;
    }
}
