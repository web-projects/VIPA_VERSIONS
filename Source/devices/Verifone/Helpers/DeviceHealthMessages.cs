using static System.ExtensionMethods;

namespace Devices.Verifone.Helpers
{
    public class DeviceHealthMessages
    {
        public enum ConsoleMessages
        {
            [StringValue("DEVICE: FIRMARE VERSION ")]
            DeviceFirmwareVersion,
            [StringValue("DEVICE: ADE-")]
            DeviceADEKey,
        }
    }
}
