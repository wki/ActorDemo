namespace ActorDemo;

/// <summary>
/// Actor System containing all actors
/// </summary>
/// <param name="message"></param>
public class ActorSystem: Actor
{
    public ActorSystem(string name)
    {
        var mailboxProcessor = new MailboxProcessor(name, null, this);
        Self = mailboxProcessor;
        mailboxProcessor.Start();
    }

    public override Task OnReceiveAsync(object message)
    {
        Console.WriteLine($"system received message: {message}");
        return Task.CompletedTask;
    }
}