namespace ActorDemo;

/// <summary>
/// Helper class for building Routers etc.
/// </summary>
public abstract class ActorBuilder: IActorBuilder
{
    public IActorRef ActorOf<T>(string name, params object[] args) where T : Actor
    {
        return BuildActors(typeof(T), name, args);
    }

    protected abstract IActorRef BuildActors(Type actorType, string name, object[] args);
}