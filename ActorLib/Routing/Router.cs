namespace ActorLib.Routing;

public class Router: Actor
{
    private readonly IRoutingStrategy _routingStrategy;
    
    public Router(Actor parent, string name, IRoutingStrategy routingStrategy, Type childType, object[] childArgs) 
        : base(parent, name)
    {
        _routingStrategy = routingStrategy;
        _routingStrategy.BuildChildren(this, childType, childArgs);
    }

    protected override Task OnReceiveAsync(object message)
    {
        Forward(_routingStrategy.ChildToForwardTo());
        return Task.CompletedTask;
    }
}