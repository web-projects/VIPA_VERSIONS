namespace Devices.Sdk
{
    public class DeviceConnectInfo
    {
        public enum DeviceConnectionType
        {
            Unknown,
            Comm,
            USB,
            TCPIP
        }

        public DeviceConnectionType ConnectionType { get; set; }
        public string Connection { get; set; }
        public int? Speed { get; set; }
        public int? Timeout { get; set; }
    }
}
