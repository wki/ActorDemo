using System.Reflection;

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

    // actor with user-provided message handling code and state
    public Actor Actor { get; }
    
    // mailbox contains unprocessed messages
    private readonly Queue<Envelope> _mailbox = new Queue<Envelope>();
    
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

        // var ctor = actorType.GetConstructors().FirstOrDefault();
        // if (ctor is null)
        //     throw new InvalidOperationException($"Actor {actorType.Name} has no constructur");
        //
        // var actor = ctor.Invoke(args)
        //     as Actor;
        
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

    // low level handling of an envelope including mailbox locking
    private void EnqueueMessage(Envelope envelope)
    {
        lock (_mailbox)
        {
            _mailbox.Enqueue(envelope);
            ProcessNextMessageIfIdle();
        }
    }

    // check queue if we can continue with a new message
    // CAUTION: must be called when inside a lock(_mailboxAccess)
    private void ProcessNextMessageIfIdle()
    {
        if (_state == ActorState.Idle && _mailbox.Any())
        {
            _currentlyProcessing = _mailbox.Dequeue();
            // Console.WriteLine($"Processing {_currentlyProcessing.Sender}->{_currentlyProcessing.Receiver}: {_currentlyProcessing.Message}");
            _state = ActorState.Running;
            Actor.Sender = _currentlyProcessing.Sender;
            Actor
                .OnReceiveAsync(_currentlyProcessing.Message)
                .ContinueWith(MessageProcessed, TaskContinuationOptions.RunContinuationsAsynchronously);
        }
    }

    // last message was processed. go on.
    private void MessageProcessed(Task messageProcessingTask)
    {
        if (messageProcessingTask.IsFaulted)
        {
            Fault(messageProcessingTask.Exception, _currentlyProcessing.Message);

            if (_restartPolicy.CanRestart())
                Restart();
            else
                Stop();
        }
        else
            IdleAndCheckForNextMessage();
    }

    // run an actor's hook ignoring exceptions
    private void RunHook(Action hook)
    {
        try { hook(); }
        catch (Exception _) { }
    }

    private void IdleAndCheckForNextMessage()
    {
        lock (_mailbox)
        {
            Idle();
            ProcessNextMessageIfIdle();
        }
    }
    #endregion

    #region State handling
    /// <summary>
    /// Start processing messages
    /// </summary>
    public void Start()
    {
        _state = ActorState.Idle;
        RunHook(() => Actor.AfterStart());
    }

    /// <summary>
    /// Idle message processing. Will wait until another message arrives
    /// </summary>
    public void Idle()
    {
        _currentlyProcessing = null;
        _state = ActorState.Idle;
    }

    /// <summary>
    /// An error occured
    /// </summary>
    /// <param name="exception"></param>
    /// <param name="message"></param>
    public void Fault(AggregateException exception, object message)
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
        IdleAndCheckForNextMessage();
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
    
    public override string ToString() => $"Actor '{Name}'";
}