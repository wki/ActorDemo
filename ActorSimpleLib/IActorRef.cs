namespace ActorSimpleLib;

public interface IActorRef
{
    string Name { get; }
    void Stop();
}