namespace Devices.Verifone.Helpers
{
    public class DeviceHealthStatus
    {
        public bool PaymentKeysAreValid { get; set; }
        public bool PackagesAreValid { get; set; }
        public bool TerminalTimeStampIsValid { get; set; }
        public bool Terminal24HoureRebootIsValid { get; set; }
        public bool EmvKernelConfigurationIsValid { get; set; }
    }
}
