namespace ActorSimpleLib;

public class ActorSystem : Actor
{
    public ActorSystem(string name) : base(null, name) { }

    protected override Task OnReceiveAsync(object message)
    {
        if (message is ChildTerminated childTerminated)
        {
            Console.WriteLine($"Child Terminated: {childTerminated.Name}");
        }
        return Task.CompletedTask;
    }
}