using Common.XO.Device;

namespace Common.XO.Requests
{
    public class LinkDeviceRequest
    {
        public LinkDALIdentifier DALIdentifier { get; set; }
        public LinkDeviceIdentifier DeviceIdentifier { get; set; }
    }
}
