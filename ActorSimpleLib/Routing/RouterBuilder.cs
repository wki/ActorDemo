namespace ActorSimpleLib.Routing;

public class RouterBuilder
{
    private readonly Actor _parent;
    private readonly IRoutingStrategy _routingStrategy;

    public RouterBuilder(Actor parent, IRoutingStrategy routingStrategy)
    {
        _parent = parent;
        _routingStrategy = routingStrategy;
    }

    public Actor ActorOf<T>(string name, params object[] childArgs) where T : Actor => 
        _parent.ActorOf<Router>(name, _routingStrategy, typeof(T), childArgs);
}