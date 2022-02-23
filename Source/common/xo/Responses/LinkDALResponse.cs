using Common.XO.Device;
using System.Collections.Generic;
using XO;

namespace Common.XO.Responses
{
    public class LinkDALResponse : LinkFutureCompatibility
    {
        public List<LinkErrorValue> Errors { get; set; }

        public LinkDALIdentifier DALIdentifier { get; set; }
        public List<LinkDeviceResponse> Devices { get; set; }
        public bool OnlineStatus { get; set; }
        public bool AvailableStatus { get; set; }
        //WorkflowControls only used when request Action = 'DALStatus'; can be null
        //public LinkWorkflowControls WorkflowControls { get; set; }
    }
}
