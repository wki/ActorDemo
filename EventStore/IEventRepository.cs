namespace EventStore;

public interface IEventRepository
{
    Task AppendAsync(Guid aggregateId, IEvent @event);
    Task<IList<IEvent>> LoadAsync(Guid aggregateId);
}