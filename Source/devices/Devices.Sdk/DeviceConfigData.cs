using System.Collections.Generic;

namespace Devices.Sdk
{
    public class DeviceConfigData
    {
        public string Manufacturer { get; set; }
        public string DynamicLibraryName { get; set; }
        public List<DeviceConnectInfo> ConnectInfos { get; } = new List<DeviceConnectInfo>();
    }
}
