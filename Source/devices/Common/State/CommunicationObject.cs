using IPA5.XO.ProtoBuf;
using LinkRequest = IPA5.XO.ProtoBuf.LinkRequest;

namespace Devices.Common.State
{
    public sealed class CommunicationObject
    {
        public LinkRequest LinkRequest { get; set; }
        public CommunicationHeader Header { get; set; }

        public CommunicationObject(CommunicationHeader header, LinkRequest request)
        {
            Header = header;
            LinkRequest = request;
        }
    }
}
