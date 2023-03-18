namespace ActorLib.Routing;

/// <summary>
/// Builder for creating a router plus child actors according to routing Strategy
/// </summary>
public class RouterBuilder: ActorBuilder
{
    private readonly MailboxProcessor _parent;
    private readonly IRoutingStrategy _routingStrategy;

    public RouterBuilder(MailboxProcessor parent, IRoutingStrategy routingStrategy)
    {
        _parent = parent;
        _routingStrategy = routingStrategy;
    }

    protected override IActorRef BuildActors(Type actorType, string name, object[] childArgs) =>
        _parent.ActorOf<Router>(name, _routingStrategy, actorType, childArgs);
}