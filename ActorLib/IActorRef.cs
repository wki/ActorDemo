namespace ActorLib;

/// <summary>
/// Reference to an actor to be used as receiver for messages
/// </summary>
public interface IActorRef
{
    void SendMessage(IActorRef sender, IActorRef receiver, object message);
}