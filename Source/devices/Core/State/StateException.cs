using Devices.Core.State.Enums;
using System;
using System.Runtime.Serialization;

namespace Devices.Core.State
{
    public class StateException : Exception
    {
        public DeviceWorkflowState ExceptionState { get; }

        public StateException()
        {
        }

        public StateException(string message, DeviceWorkflowState state) : base(message)
        {
            ExceptionState = state;
        }

        public StateException(string message) : base(message)
        {
        }

        public StateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public StateException(string message, Exception innerException, DeviceWorkflowState state)
            : base(message, innerException)
        {
            ExceptionState = state;
        }

        protected StateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
