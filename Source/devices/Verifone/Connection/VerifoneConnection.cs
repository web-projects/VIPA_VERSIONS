using Devices.Common;
using Devices.Verifone.VIPA;

namespace Devices.Verifone.Connection
{
    public class VerifoneConnection : IVerifoneConnection
    {
        #region --- attributes ---
        public SerialConnection serialConnection { get; set; }

        #endregion

        #region --- public methods ---

        public bool Connect(DeviceInformation deviceInformation, DeviceLogHandler deviceLogHandler, bool exposeExceptions = false)
        {
            serialConnection = new SerialConnection(deviceInformation, deviceLogHandler);
            return serialConnection?.Connect() ?? false;
        }

        public bool IsConnected()
        {
            return serialConnection?.IsConnected() ?? false;
        }

        public void Disconnect()
        {
            serialConnection?.Disconnect();
        }

        public void Dispose()
        {
            serialConnection?.Dispose();
            serialConnection = null;
        }

        public void WriteSingleCmd(VIPAResponseHandlers responsehandlers, VIPACommand command)
        {
            serialConnection?.WriteSingleCmd(responsehandlers, command);
        }

        public void WriteRaw(byte[] buffer, int length)
        {
            serialConnection?.WriteRaw(buffer, length);
        }

        public void WriteChainedCmd(VIPAResponseHandlers responsehandlers, VIPACommand command)
        {
            serialConnection?.WriteChainedCmd(responsehandlers, command);
        }
        #endregion
    }
}
