using Devices.Common.Config;
using System;

namespace Devices.Common.AppConfig
{
    [Serializable]
    public class DeviceSection
    {
        public string DefaultDevicePort { get; set; }
        public VerifoneSettings Verifone { get; internal set; } = new VerifoneSettings();
        public IdTechSettings IdTech { get; internal set; } = new IdTechSettings();
        public SimulatorSettings Simulator { get; internal set; } = new SimulatorSettings();
        public NoDeviceSettings NoDevice { get; internal set; } = new NoDeviceSettings();
        public int DeviceDiscoveryDelay { get; set; } = 5;
    }
}
