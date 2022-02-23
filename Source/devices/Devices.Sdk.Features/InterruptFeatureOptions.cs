using Devices.Common;
using Devices.Sdk.Features.State;
using Common.XO.ProtoBuf;
using LinkRequest = XO.Requests.LinkRequest;

namespace Devices.Sdk.Features
{
    public sealed class InterruptFeatureOptions
    {
        public const int InterruptFeatureNoTimeout = -1;
        public const int InterruptFeatureDefaultTimeout = 5000;

        public IDALSubStateController Controller { get; private set; }
        public LinkRequest Request { get; private set; }
        public ICardDevice TargetDevice { get; private set; }

        public CommunicationHeader Header { get; private set; }

        public InterruptFeatureOptions SetController(IDALSubStateController controller)
        {
            Controller = controller;
            return this;
        }

        public InterruptFeatureOptions SetRequest(CommunicationHeader header, LinkRequest request)
        {
            Header = header;
            Request = request;
            return this;
        }

        public InterruptFeatureOptions SetTargetDevice(ICardDevice device)
        {
            TargetDevice = device;
            return this;
        }
    }
}
