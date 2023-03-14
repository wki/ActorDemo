namespace ActorDemo;

/// <summary>
/// Base Class for every Actor and ActorSystem (contains user code for actor)
/// </summary>
public abstract class Actor
{
    /// <summary>
    /// Sender of the message currently processed
    /// </summary>
    public IActorRef Sender { get; set; }

    /// <summary>
    /// my own reference
    /// </summary>
    protected IActorRef Self { get; set; }
    
    private MailboxProcessor MyMailboxProcessor => Self as MailboxProcessor; 

    protected string Name => MyMailboxProcessor.Name;

    protected IActorRef Parent => MyMailboxProcessor.Parent;

    protected IReadOnlyList<IActorRef> Children => MyMailboxProcessor.Children;

    protected Actor() {}

    /// <summary>
    /// Build a (child) actor
    /// </summary>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IActorRef ActorOf<T>(string name, params object[] args)
        where T : Actor
    {
        var actor = typeof(T)
                .GetConstructor(args.Select(a => a.GetType()).ToArray())
                .Invoke(args)
            as T;
        
        var mailboxProcessor = new MailboxProcessor(name, MyMailboxProcessor, actor);
        actor.Self = mailboxProcessor;
        mailboxProcessor.Start();
        
        return actor.Self;
    }

    public void AfterStart() {}
    public void BeforeStop() {}
    public void BeforeRestart(Exception e, object message) {}
    public void AfterRestart() {}

    /// <summary>
    /// must be implemented to handle a single message async
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public abstract Task OnReceiveAsync(object message);

    /// <summary>
    /// Send a message to a receiving actor
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="message"></param>
    public void Tell(IActorRef receiver, object message) =>
        receiver.SendMessage(Self, receiver, message);

    public override string ToString() => MyMailboxProcessor.ToString();
}
