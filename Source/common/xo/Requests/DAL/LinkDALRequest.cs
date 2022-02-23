using Common.XO.Device;
using Common.XO.Private;

namespace Common.XO.Requests.DAL
{
    public class LinkDALRequest
    {
        public LinkDALIdentifier DALIdentifier { get; set; }

        public LinkDeviceIdentifier DeviceIdentifier { get; set; }

        public LinkDALRequestIPA5Object LinkObjects { get; set; }
    }
}
