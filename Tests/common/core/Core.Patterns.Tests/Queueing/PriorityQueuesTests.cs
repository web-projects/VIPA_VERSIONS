using Common.Core.Patterns.Queuing;
using Devices.Core.State.Actions.Preprocessing;
using System;
using System.Diagnostics;
using Xunit;

namespace Common.Core.Patterns.Tests.Queueing
{
    public class PriorityQueuesTests
    {
        readonly PriorityQueue<PriorityQueueDeviceEvents> subject;

        public PriorityQueuesTests()
        {
            subject = new PriorityQueue<PriorityQueueDeviceEvents>();

            PriorityQueueDeviceEvents add1 = new PriorityQueueDeviceEvents(PriorityEventType.CommEvent, (int)PriorityEventType.CommEvent);
            PriorityQueueDeviceEvents add2 = new PriorityQueueDeviceEvents(PriorityEventType.CancelationRequest, (int)PriorityEventType.CancelationRequest);
            PriorityQueueDeviceEvents add3 = new PriorityQueueDeviceEvents(PriorityEventType.Timeout, (int)PriorityEventType.Timeout);
            PriorityQueueDeviceEvents add4 = new PriorityQueueDeviceEvents(PriorityEventType.UserCancel, (int)PriorityEventType.UserCancel);
            PriorityQueueDeviceEvents add5 = new PriorityQueueDeviceEvents(PriorityEventType.DeviceReport, (int)PriorityEventType.DeviceReport);

            subject.Enqueue(add3);
            subject.Enqueue(add4);
            subject.Enqueue(add1);
            subject.Enqueue(add5);
            subject.Enqueue(add2);
        }

        [Theory]
        [InlineData(PriorityEventType.CommEvent, PriorityEventType.CancelationRequest, 100)]
        [InlineData(PriorityEventType.CommEvent, PriorityEventType.CancelationRequest, 1000)]
        [InlineData(PriorityEventType.CommEvent, PriorityEventType.CancelationRequest, 10000)]
        [InlineData(PriorityEventType.CommEvent, PriorityEventType.CancelationRequest, 100000)]
        public void Enqueue_AddsRecordsInaTimelyFashion_WhenCalled(PriorityEventType firstEvent, PriorityEventType lastEvent, int numberOfIterations)
        {
            PriorityQueue<PriorityQueueDeviceEvents> testQueue = new PriorityQueue<PriorityQueueDeviceEvents>();
            Random random = new Random();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int index = 0; index < numberOfIterations; index++)
            {
                int randomEvent = random.Next((int)firstEvent, (int)lastEvent);

                if (Enum.IsDefined(typeof(PriorityEventType), randomEvent))
                {
                    PriorityQueueDeviceEvents priorityEvent = new PriorityQueueDeviceEvents((PriorityEventType)randomEvent, index);
                    testQueue.Enqueue(priorityEvent);
                }
            }

            Assert.True(testQueue.Count() == numberOfIterations);

            for (int i = 0; i < numberOfIterations; i++)
            {
                _ = testQueue.Dequeue();
            }

            sw.Stop();

            Assert.True(testQueue.Count() == 0);
            Assert.True(sw.ElapsedMilliseconds < 1000);
        }

        [Theory]
        [InlineData(PriorityEventType.CommEvent, PriorityEventType.CancelationRequest)]
        [InlineData(PriorityEventType.CancelationRequest, PriorityEventType.Timeout)]
        [InlineData(PriorityEventType.Timeout, PriorityEventType.UserCancel)]
        [InlineData(PriorityEventType.UserCancel, PriorityEventType.DeviceReport)]
        public void Enqueue_ReordersElementsByPriority_WhenCalled(PriorityEventType highPriority, PriorityEventType lowPriority)
        {
            PriorityQueue<PriorityQueueDeviceEvents> testQueue = new PriorityQueue<PriorityQueueDeviceEvents>();

            PriorityQueueDeviceEvents hiPriorityEvent = new PriorityQueueDeviceEvents(highPriority, (int)highPriority);
            testQueue.Enqueue(hiPriorityEvent);

            PriorityQueueDeviceEvents lowPriorityEvent = new PriorityQueueDeviceEvents(lowPriority, (int)lowPriority);
            testQueue.Enqueue(lowPriorityEvent);

            PriorityQueueDeviceEvents head = testQueue.Peek();

            Assert.Equal(hiPriorityEvent, head);
        }

        [Fact]
        public void Dequeue_ShouldRemoveHighestPriorityEvent_WhenCalled()
        {
            PriorityQueueDeviceEvents delete1 = subject.Dequeue();
            Assert.Equal(PriorityEventType.CommEvent, delete1.eventType);
            Assert.True(subject.IsConsistent());
        }

        [Fact]
        public void Dequeue_AddingALowerPriorityEventAndDequeing_ShouldStillRemoveHighestPriorityItem_WhenCalled()
        {
            PriorityQueueDeviceEvents add1 = new PriorityQueueDeviceEvents(PriorityEventType.Timeout, (int)PriorityEventType.Timeout);
            subject.Enqueue(add1);
            PriorityQueueDeviceEvents delete1 = subject.Dequeue();
            Assert.NotEqual(add1.Priority, delete1.Priority);
            Assert.True(subject.IsConsistent());
        }
    }
}
