namespace ActorSimpleLib;

public class ActorSystem : Actor
{
    public ActorSystem(string name) : base(null, name) { }

    protected override Task OnReceiveAsync(object message) => 
        Task.CompletedTask;
}