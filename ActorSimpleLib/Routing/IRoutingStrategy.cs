namespace ActorSimpleLib.Routing;

public interface IRoutingStrategy
{
    void BuildChildren(Actor router, Type childType, object[] childArgs);
    IActorRef ChildToForwardTo();
}