using System.Threading.Channels;

namespace ActorDemo;

/// <summary>
/// Working logic behind Actor and ActorSystem
/// </summary>
/// <remarks>
/// * handles incoming messages
/// * handles processing of oldest message
/// * handles errors during message processing
/// * handles child-actor creation
/// </remarks>
public class MailboxProcessor: IActorRef
{
    // current state of the actor
    private ActorState _state = ActorState.Initializing;
    
    // internal list of Children
    private readonly Dictionary<string, MailboxProcessor> _children = new Dictionary<string, MailboxProcessor>();

    /// <summary>
    /// Access all children of this actor
    /// </summary>
    public IReadOnlyList<IActorRef> Children => _children.Values.ToList().AsReadOnly();
    
    /// <summary>
    /// This actor's parent (system has null as parent)
    /// </summary>
    public MailboxProcessor Parent { get; }
    
    /// <summary>
    /// Actor's name (unique among siblings)
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Handles restarting an actor in case of failure possibly with delays
    /// </summary>
    private readonly IRestartPolicy _restartPolicy;

    // we are using an unbounded channel as a mailbox
    private Channel<Envelope> _mailbox = Channel.CreateUnbounded<Envelope>();
    
    // the task which processes our messages
    private Task _messageLoop;
    
    // actor with user-provided message handling code and state
    public Actor Actor { get; }
    
    // stash contains messages to be processed defered when unstashed
    public readonly Queue<Envelope> _stash = new Queue<Envelope>();
    
    // currently processed message
    private Envelope? _currentlyProcessing = null;
    
    public MailboxProcessor(string name, MailboxProcessor parent, Actor actor)
    {
        // system has no parent
        parent?.AddChild(name, this);

        Name = name;
        Parent = parent;
        Actor = actor;
        _restartPolicy = new DelayedRestartPolicy();
    }
    
    #region Actor creation
    public IActorRef ActorOf<T>(string name, params object[] args)
        where T : Actor => ActorOf(typeof(T), name, args);
    
    public IActorRef ActorOf(Type actorType, string name, params object[] args)
    {
        if (name.Contains('*'))
        {
            var randomPart = 
                Path.GetRandomFileName()
                    .Replace(".", "")
                    .Substring(0, 8);
            name = name.Replace("*", randomPart);
        }

        // take first constructor
        // var ctor = actorType.GetConstructors().FirstOrDefault();
        // if (ctor is null)
        //     throw new InvalidOperationException($"Actor {actorType.Name} has no constructur");
        //
        // var actor = ctor.Invoke(args)
        //     as Actor;
        
        // find matching constructor
        var actor = actorType
                .GetConstructor(args.Select(a => a.GetType()).ToArray())
                .Invoke(args)
            as Actor;
        
        var mailboxProcessor = new MailboxProcessor(name, this, actor);
        actor.Self = mailboxProcessor;
        mailboxProcessor.Start();
        
        return mailboxProcessor;
    }
    #endregion

    #region Message handling
    /// <summary>
    /// Low level message sending with all parts of an envelope (not recommended)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="receiver"></param>
    /// <param name="message"></param>
    public void SendMessage(IActorRef sender, IActorRef receiver, object message) =>
        EnqueueMessage(new Envelope(sender, receiver, message));

    /// <summary>
    /// Reploy with a new message to the sender of the current message
    /// </summary>
    /// <param name="message"></param>
    public void Reply(object message) =>
        _currentlyProcessing.Sender.SendMessage(this, _currentlyProcessing.Sender, message);

    /// <summary>
    /// Forward the currently processed message to someone else
    /// </summary>
    /// <param name="receiver"></param>
    public void Forward(IActorRef receiver) =>
        receiver.SendMessage(_currentlyProcessing.Sender, receiver, _currentlyProcessing.Message);

    // low level handling of an envelope
    private void EnqueueMessage(Envelope envelope)
    {
        if (!_mailbox.Writer.TryWrite(envelope))
            Console.WriteLine($"{this}: could not enqueue Message");
    }

    private async Task BuildAndRunMessageLoop()
    {
        while (_state != ActorState.Stopped)
        {
            _state = ActorState.Idle;
            _currentlyProcessing = await _mailbox.Reader.ReadAsync();
            _state = ActorState.Running;
            Actor.Sender = _currentlyProcessing.Sender;
            try
            {
                await Actor.OnReceiveAsync(_currentlyProcessing.Message);
            }
            catch (Exception e)
            {
                if (await _restartPolicy.CanRestartAsync())
                {
                    HandleFault(e, _currentlyProcessing.Message);
                    Restart();
                }
                else
                    // Restart Policy forbids a restart.
                    break;
            }
        }

        Stop();
    }
    #endregion

    #region State handling
    private void RunHook(Action hook)
    {
        try { hook(); }
        catch (Exception _) { }
    }

    /// <summary>
    /// Start processing messages
    /// </summary>
    public void Start()
    {
        _state = ActorState.Idle;
        RunHook(() => Actor.AfterStart());
        _messageLoop = BuildAndRunMessageLoop();
    }

    /// <summary>
    /// An error occured
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="message"></param>
    public void HandleFault(Exception exception, object message)
    {
        _state = ActorState.Faulty;
        RunHook(() => Actor.BeforeRestart(exception, message));
    }

    /// <summary>
    /// Restart the actor after clearing the error
    /// </summary>
    public void Restart()
    {
        _state = ActorState.Restarting;
        RunHook(() => Actor.AfterRestart());
    }
    
    /// <summary>
    /// Stop processing messages
    /// </summary>
    public void Stop()
    {
        RunHook(() => Actor.BeforeStop());
        Parent?.RemoveChild(Name);
        _state = ActorState.Stopped;
    }
    #endregion
    
    #region Child handling
    public MailboxProcessor GetChild(string name)
    {
        lock (_children)
        {
            if (_children.ContainsKey(name))
                return _children[name];
            return null;
        }
    }

    public void AddChild(string name, MailboxProcessor mailboxProcessor)
    {
        lock (_children)
        {
            if (_children.ContainsKey(name))
                throw new ArgumentException($"Actor name '{name}' is already present");
            _children.Add(name, mailboxProcessor);
        }
    }

    public void RemoveChild(string name)
    {
        lock (_children)
        {
            _children.Remove(name);
        }
    }
    #endregion
    
    #region Stash handling
    /// <summary>
    /// put the currently processing message to a stash (typically in BeforeRestart hook)
    /// </summary>
    public void Stash() =>
        _stash.Enqueue(_currentlyProcessing);

    /// <summary>
    /// Emoty the entire stash
    /// </summary>
    public void ClearStash() =>
        _stash.Clear();

    /// <summary>
    /// Recover all stashed messages into the actor's Mailbox
    /// </summary>
    public void UnStashAll()
    {
        while (_stash.Any())
            EnqueueMessage(_stash.Dequeue());
    }
    #endregion
    
    public override string ToString() => $"ActorRef '{Name}'";
}