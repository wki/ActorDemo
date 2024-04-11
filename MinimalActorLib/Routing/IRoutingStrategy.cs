namespace MinimalActorLib.Routing;

public interface IRoutingStrategy
{
    void BuildChildren(Actor router);
    Actor ChildToForwardTo();
}