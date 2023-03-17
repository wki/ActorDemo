namespace ActorDemo.Routing;

/// <summary>
/// A router with a given number of children handling requests in round-robin fashion
/// </summary>
public class RoundRobinPool: IRoutingStrategy
{
    private readonly int _nrChildren;
    private int _childIndex;
    private MailboxProcessor _router;

    public RoundRobinPool(int nrChildren)
    {
        _nrChildren = nrChildren;
        _childIndex = 0;
    }

    public void BuildChildren(IActorRef router, Type actorType, object[] childArgs)
    {
        _router = (MailboxProcessor) router;
        var name = $"{_router.Name}-*";
        for (var i = 1; i <= _nrChildren; i++)
            _router.ActorOf(actorType, name, childArgs);
    }

    public Task OnReceiveAsync(object message)
    {
        var child = _router.Children[_childIndex];
        _router.Actor.Forward(child);
        _childIndex = (_childIndex + 1) % _nrChildren;
        
        return Task.CompletedTask;
    }
}