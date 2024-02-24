namespace MinimalActorLib.Routing;

public class RoundRobinPool: IRoutingStrategy
{
    private readonly int _nrChildren;
    private int _childIndex;
    private readonly List<Actor> _children;

    public RoundRobinPool(int nrChildren)
    {
        _nrChildren = nrChildren;
        _childIndex = 0;
        _children = new List<Actor>();
    }

    public void BuildChildren(Actor router, Type childType, object[] childArgs)
    {
        foreach (var childToRemove in _children.Where(c => c.ActorStatus >= ActorStatus.Stopping))
            _children.Remove(childToRemove);
        
        while (_children.Count < _nrChildren)
            _children.Add(router.ActorOf(childType, childArgs));
    }

    public Actor ChildToForwardTo()
    {
        _childIndex = (_childIndex + 1) % _children.Count;
        return _children[_childIndex];
    }
}
