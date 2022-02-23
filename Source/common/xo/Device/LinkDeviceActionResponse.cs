using Common.XO.Responses;
using System.Collections.Generic;

namespace Common.XO.Device
{
    public class LinkDeviceActionResponse
    {
        public string Status { get; set; }
        public List<LinkErrorValue> Errors { get; set; }
    }
}
