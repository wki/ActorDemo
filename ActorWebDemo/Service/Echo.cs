using ActorLib;

namespace ActorWebDemo.Service;

public class Echo : Actor
{
    private int _count = 0;

    public Echo(Actor parent, string name): base(parent, name)
    {
    }
    
    protected override Task OnReceiveAsync(object message)
    {
        Reply($"Echo: {message} ({++_count})");
        return Task.CompletedTask;
    }
}