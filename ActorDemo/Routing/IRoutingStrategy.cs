namespace ActorDemo.Routing;

public interface IRoutingStrategy
{
    void BuildChildren(IActorRef router, Type actorType, object[] childArgs);
    Task OnReceiveAsync(object message);
}