namespace Dessentials.Common.EventBus
{
    /// <summary>
    /// Marker interface for all events raised through the EventBus.
    /// Implement as a struct to avoid GC allocations.
    /// </summary>
    public interface IEvent { }
}
