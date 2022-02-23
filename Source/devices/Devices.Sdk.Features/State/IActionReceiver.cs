using Devices.Common;
using Devices.Common.Helpers;
using System.Threading.Tasks;
using LinkRequest = Common.XO.Requests.LinkRequest;

namespace Devices.Sdk.Features.State
{
    public interface IActionReceiver
    {
        //void RequestReceived(CommunicationHeader header, LinkRequest request);
        void DeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation);
        Task ComportEventReceived(PortEventType comPortEvent, string portNumber);
        bool RequestSupported(LinkRequest request);
    }
}
