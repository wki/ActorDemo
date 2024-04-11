namespace MinimalActorLib;

public class ActorSystem: Actor
{
    public ActorSystem()
    {
        // prevent Mailbox from filling endlessly
        EventLoop();
    }

    protected override Task OnReceiveAsync(object message)
    {
        Console.WriteLine($"ActorSystem: received {message}");
        return Task.CompletedTask;
    }
}