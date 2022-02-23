using Common.XO.Requests;
using Devices.Common;
using Devices.Common.Helpers;
using System.Threading.Tasks;

namespace Devices.Core.State.Interfaces
{
    internal interface IActionReceiver
    {
        void RequestReceived(LinkRequest request);
        object DeviceEventReceived(DeviceEvent deviceEvent, DeviceInformation deviceInformation);
        Task ComportEventReceived(PortEventType comPortEvent, string portNumber);
    }
}
