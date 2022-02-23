using System;

namespace Devices.Sdk.Features.Interrupt
{
    internal sealed class InterruptManagementException : Exception
    {
        public InterruptManagementException()
            : base()
        { }

        public InterruptManagementException(string message)
            : base(message)
        { }
    }
}
