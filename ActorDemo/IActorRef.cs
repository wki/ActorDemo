namespace ActorDemo;

public interface IActorRef
{
    void SendMessage(IActorRef sender, IActorRef receiver, object message);
}