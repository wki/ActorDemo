namespace MinimalActorLib.Routing;

public class RoundRobinPool: IRoutingStrategy
{
    private readonly int _nrChildren;
    private readonly Type _childType;
    private readonly object[] _childArgs;
    private int _childIndex;
    private readonly List<Actor> _children;

    public RoundRobinPool(int nrChildren, Type childType, object[] childArgs)
    {
        _nrChildren = nrChildren;
        _childType = childType;
        _childArgs = childArgs;
        _childIndex = 0;
        _children = new List<Actor>();
    }

    public void BuildChildren(Actor router)
    {
        foreach (var childToRemove in _children.Where(c => c.ActorStatus >= ActorStatus.Stopping))
            _children.Remove(childToRemove);
        
        while (_children.Count < _nrChildren)
            _children.Add(router.ActorOf(_childType, _childArgs));
    }

    public Actor ChildToForwardTo() => 
        _children[_childIndex = (_childIndex + 1) % _children.Count];
}
