namespace EventStore;

public interface IEventRepository
{
    void Append(int aggregateId, int version, IEvent @event);
    IList<IEvent> Load(int aggregateId);
}