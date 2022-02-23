using Devices.Common.Helpers;
using System;

namespace Devices.Core.SerialPort
{
    public sealed class PortsChangedArgs : EventArgs
    {
        public string[] SerialPorts { get; }
        public PortEventType EventType { get; }

        public PortsChangedArgs(PortEventType eventType, string[] serialPorts)
            => (EventType, SerialPorts) = (eventType, serialPorts);
    }
}
