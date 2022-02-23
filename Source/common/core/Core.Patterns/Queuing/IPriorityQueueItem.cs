namespace Common.Core.Patterns.Queuing
{
    public interface IPriorityQueueItem
    {
        int Priority { get; set; }
    }
}
