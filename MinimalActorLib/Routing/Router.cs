namespace MinimalActorLib.Routing;

public class Router: Actor
{
    private readonly IRoutingStrategy _routingStrategy;

    public Router(IRoutingStrategy routingStrategy)
    {
        _routingStrategy = routingStrategy;
    }

    protected override Task OnReceiveAsync(object message)
    {
        _routingStrategy.BuildChildren(this);
        Forward(_routingStrategy.ChildToForwardTo());
        return Task.CompletedTask;
    }
}