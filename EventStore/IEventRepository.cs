namespace EventStore;

public interface IEventRepository
{
    Task AppendAsync(int aggregateId, int version, IEvent @event);
    Task<IList<IEvent>> LoadAsync(int aggregateId);
}