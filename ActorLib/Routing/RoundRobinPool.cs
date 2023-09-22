namespace ActorLib.Routing;

public class RoundRobinPool: IRoutingStrategy
{
    private readonly int _nrChildren;
    private int _childIndex;
    private Actor _router;

    public RoundRobinPool(int nrChildren)
    {
        _nrChildren = nrChildren;
        _childIndex = 0;
    }

    public void BuildChildren(Actor router, Type childType, object[] childArgs)
    {
        _router = router;
        var name = $"{_router.Name}-*";
        while (_router.Children.Count < _nrChildren)
            _router.ActorOf(name, childType, childArgs);
    }

    public Actor ChildToForwardTo()
    {
        _childIndex = (_childIndex + 1) % _nrChildren;
        return _router.Children[_childIndex];
    }
}