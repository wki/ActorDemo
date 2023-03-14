namespace ActorDemo;

public interface IActorRef
{
    void SendMessage(IActorRef sender, IActorRef receiver, object message);
}

public record Envelope(IActorRef Sender, IActorRef Receiver, object Message);

public class MailboxProcessor: IActorRef
{
    public IActorRef Parent;
    public string Name { get; }
    private readonly Actor _actor;
    private readonly Queue<Envelope> _mailbox = new Queue<Envelope>();
    private Envelope? _currentlyProcessing = null;
    // semaphore for locking
    private object _mailboxAccess = new object();

    public MailboxProcessor(string name, IActorRef parent, Actor actor)
    {
        Name = name;
        Parent = parent;
        _actor = actor;
    }

    public void SendMessage(IActorRef sender, IActorRef receiver, object message) =>
        EnqueueMessage(new Envelope(sender, receiver, message));

    private void EnqueueMessage(Envelope envelope)
    {
        lock (_mailboxAccess)
        {
            _mailbox.Enqueue(envelope);
            ProcessNextMessage();
        }
    }

    // check queue if we can continue with a new message
    // CAUTION: must be called when inside a lock(_mailboxAccess)
    private void ProcessNextMessage()
    {
        if (_currentlyProcessing is null)
        {
            _currentlyProcessing = _mailbox.Dequeue();
            _actor.Sender = _currentlyProcessing.Sender;
            _actor
                .OnReceiveAsync(_currentlyProcessing.Message)
                .ContinueWith(MessageProcessed);
        }
    }

    // last message was processed. go on.
    private void MessageProcessed(Task task)
    {
        if (task.IsCanceled)
        {
            // currently impossible
        }
        else if (task.IsFaulted)
        {
            // TODO: error handling
        }
        else
        {
            lock (_mailboxAccess)
            {
                _currentlyProcessing = null;
                ProcessNextMessage();
            }
        }
    }

    public override string ToString() => $"Actor '{Name}'";
}

/// <summary>
/// Base Class for every Actor and ActorSystem (contains user code)
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
        
        actor.Self = new MailboxProcessor(name, Self, actor);
        return actor.Self;
    }

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

/// <summary>
/// Actor System containing all actors
/// </summary>
/// <param name="message"></param>
public class ActorSystem: Actor
{
    public ActorSystem(string name)
    {
        Self = new MailboxProcessor(name, null, this);
    }

    public override Task OnReceiveAsync(object message)
    {
        Console.WriteLine($"system received message: {message}");
        return Task.CompletedTask;
    }
}
