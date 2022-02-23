using Common.XO.ProtoBuf;
using LinkRequest = XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.State
{
    public sealed class WorkflowOptions
    {
        public int? ExecutionTimeout;
        public LinkRequest LinkRequest;
        public IDeviceWorkflowFeature TargetFeature;
        public CommunicationHeader Header;
    }
}
