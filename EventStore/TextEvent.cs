namespace EventStore;

public class TextEvent : IEvent
{
    public string Info { get; set; }
}