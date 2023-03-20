namespace ActorLib.Tests;

public class SendActor : Actor
{
    private readonly IActorRef _other;

    public SendActor(IActorRef other)
    {
        _other = other;
    }
    
    public override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case "tell":
                Tell(_other, new Ping());
                break;
            
            case Ping:
                Reply(new Pong());
                break;

            default:
                Forward(_other);
                break;
        }

        return Task.CompletedTask;
    }
}