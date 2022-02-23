using Devices.Common;
using Devices.Verifone.VIPA;

namespace Devices.Verifone.Connection
{
    interface IVerifoneConnection
    {
        bool Connect(DeviceInformation deviceInformation, DeviceLogHandler deviceLogHandler, bool exposeExceptions = false);
        void Disconnect();
        void WriteSingleCmd(VIPAResponseHandlers responsehandlers, VIPACommand command);
    }
}
