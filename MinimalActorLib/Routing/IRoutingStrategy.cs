namespace MinimalActorLib.Routing;

public interface IRoutingStrategy
{
    void BuildChildren(Actor router, Type childType, object[] childArgs);
    Actor ChildToForwardTo();
}