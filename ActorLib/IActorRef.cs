namespace ActorLib;

/// <summary>
/// Reference to an actor to be used as receiver for messages
/// </summary>
/// <remarks>
/// only handles a minimum of methods to enforce using messages
/// sent to an actor instead of imperative OO treatment
/// </remarks>
public interface IActorRef
{
    string Name { get; }
    
    void SendMessage(IActorRef sender, IActorRef receiver, object message);

    void Stop();
}
