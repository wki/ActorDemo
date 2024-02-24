namespace MinimalActorLib.Routing;

public class Router: Actor
{
    private readonly IRoutingStrategy _routingStrategy;
    private readonly Type _childType;
    private readonly object[] _childArgs;

    public Router(IRoutingStrategy routingStrategy, Type childType, params object[] childArgs)
    {
        _routingStrategy = routingStrategy;
        _childType = childType;
        _childArgs = childArgs;
    }

    protected override Task OnReceive(object message)
    {
        _routingStrategy.BuildChildren(this, _childType, _childArgs);
        Forward(_routingStrategy.ChildToForwardTo());
        return Task.CompletedTask;
    }
}