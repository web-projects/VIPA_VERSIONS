using Common.Core.Patterns.Queuing;
using System;

namespace Devices.Core.State.Actions.Preprocessing
{
    public class PriorityQueueDeviceEvents : IComparable<PriorityQueueDeviceEvents>, IPriorityQueueItem
    {
        public PriorityEventType eventType { get; }
        public int Priority { get; set; }

        public PriorityQueueDeviceEvents(PriorityEventType evenType, int priority)
        {
            this.eventType = evenType;
            this.Priority = priority;
        }

        public override string ToString() => $"({eventType}, {Priority.ToString("F1")})";

        public int CompareTo(PriorityQueueDeviceEvents other)
        {
            if (this.Priority < other.Priority)
            {
                return -1;
            }
            else if (this.Priority > other.Priority)
            {
                return 1;
            }

            return 0;
        }
    }
}
