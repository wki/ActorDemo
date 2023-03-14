using System.Collections.Immutable;

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
    private ActorState _state = ActorState.Initializing;
    
    // internal list of Children
    private readonly Dictionary<string, MailboxProcessor> _children = new Dictionary<string, MailboxProcessor>();

    /// <summary>
    /// Access all children of this actor
    /// </summary>
    public IReadOnlyList<IActorRef> Children => _children.Values.ToImmutableList().AsReadOnly();
    
    /// <summary>
    /// This actor's parent (system has null as parent)
    /// </summary>
    public MailboxProcessor Parent { get; }
    
    /// <summary>
    /// Actor's name (unique among siblings)
    /// </summary>
    public string Name { get; }

    // actor with user-provided message handling code and state
    private readonly Actor _actor;
    
    // mailbox contains unprocessed messages
    private readonly Queue<Envelope> _mailbox = new Queue<Envelope>();
    
    // currently processed message
    private Envelope? _currentlyProcessing = null;
    
    public MailboxProcessor(string name, MailboxProcessor parent, Actor actor)
    {
        // system has no parent
        parent?.AddChild(name, this);

        Name = name;
        Parent = parent;
        _actor = actor;
    }

    #region Message handling
    public void SendMessage(IActorRef sender, IActorRef receiver, object message) =>
        EnqueueMessage(new Envelope(sender, receiver, message));

    private void EnqueueMessage(Envelope envelope)
    {
        lock (_mailbox)
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
            if (_mailbox.Any())
            {
                _currentlyProcessing = _mailbox.Dequeue();
                _state = ActorState.Running;
                _actor.Sender = _currentlyProcessing.Sender;
                _actor
                    .OnReceiveAsync(_currentlyProcessing.Message)
                    .ContinueWith(MessageProcessed);
            }
            else
            {
                _state = ActorState.Idle;
            }
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
            var exception = task.Exception;
            _state = ActorState.Faulty;
            try
            {
                _actor.BeforeRestart(exception, _currentlyProcessing);
            }
            catch (Exception _)
            {
            }

            try
            {
                _actor.AfterRestart();
            }
            catch (Exception _)
            {
            }

            CheckForNextMessage();
        }
        else
        {
            CheckForNextMessage();
        }
    }

    private void RunHook(Action hook)
    {
        
    }

    private void CheckForNextMessage()
    {
        lock (_mailbox)
        {
            _currentlyProcessing = null;
            _state = ActorState.Idle;
            ProcessNextMessage();
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
        _actor.AfterStart();
    }
    
    /// <summary>
    /// Stop processing messages
    /// </summary>
    public void Stop()
    {
        _actor.BeforeStop();
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
    
    public override string ToString() => $"Actor '{Name}'";
}