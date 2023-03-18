namespace ActorLib.Routing;

/// <summary>
/// Base class for a router
/// </summary>
public class Router: Actor
{
    private readonly IRoutingStrategy _routingStrategy;
    private readonly Type _actorType;
    private readonly object[] _childArgs;

    public Router(IRoutingStrategy routingStrategy, Type actorType, object[] childArgs)
    {
        _routingStrategy = routingStrategy;
        _actorType = actorType;
        _childArgs = childArgs;
    }

    public override void AfterStart()
    {
        _routingStrategy.BuildChildren(MyMailboxProcessor, _actorType, _childArgs);
    }

    public override Task OnReceiveAsync(object message) =>
        _routingStrategy.OnReceiveAsync(message);
}