using System.Collections.Concurrent;
using System.Threading.Channels;
using ActorSimpleLib.Routing;

namespace ActorSimpleLib;

public abstract class Actor: IActorRef
{
    public string Name { get; private set; }
    internal IActorRef Parent { get; private set; }
    protected IActorRef Self => this;
    private readonly ConcurrentDictionary<string, Actor> _children;
    internal IReadOnlyList<IActorRef> Children => _children.Values.ToList().AsReadOnly();
    protected IActorRef Sender { get; private set; }
    private readonly Channel<Envelope> _mailbox;
    private Envelope? _currentlyProcessing;
    private Task _messageProcessingTask;
    private CancellationTokenSource _cancellationTokenSource;
    private IRestartPolicy _restartPolicy;

    protected Actor(IActorRef parent, string name)
    {
        Parent = parent;
        Name = name;
        Sender = NullActor.Instance;

        _children = new ConcurrentDictionary<string, Actor>();
        _mailbox = Channel.CreateUnbounded<Envelope>();
        _cancellationTokenSource = new CancellationTokenSource();
        _restartPolicy = new DelayedRestartPolicy();
        _messageProcessingTask = RunMessageLoopAsync();
    }
    
    #region Message Processing
    protected abstract Task OnReceiveAsync(object message);

    public void Tell(IActorRef receiver, object message) =>
        EnqueueEnvelope(this, receiver, message);

    protected void Reply(object message) =>
        EnqueueEnvelope(this, Sender, message);

    protected internal void Forward(IActorRef receiver) =>
        EnqueueEnvelope(Sender, receiver, _currentlyProcessing.Message);

    public Task<T> Ask<T>(IActorRef receiver, object message, int timeOutMillis = 500)
    {
        var answer = new TaskCompletionSource<T>();
        var askActor = ActorOf<AskActor<T>>("ask-*", receiver, message, answer, timeOutMillis);
        return answer.Task;
    }
    
    private void EnqueueEnvelope(IActorRef sender, IActorRef receiver, object message) =>
        EnqueueEnvelope(new Envelope(sender, receiver, message));
    
    private void EnqueueEnvelope(Envelope envelope)
    {
        // FIXME: exception when write fails?
        if (!((Actor)envelope.Receiver)._mailbox.Writer.TryWrite(envelope))
            Console.WriteLine($"{this}: could not enqueue Message");
    }

    // process all messages in the entire life of the actor
    private async Task RunMessageLoopAsync()
    {
        bool isRestart = false;
        bool shouldStop = false;
        while (!shouldStop)
        {
            shouldStop = await ProcessMessages(isRestart);
            isRestart = true;
        }
        
        if (Parent is not null)
        {
            this.EnqueueEnvelope(
                sender: NullActor.Instance,
                receiver: Parent,
                message: new ChildTerminated(Name)
            );
        }
    }
    
    // process messages until an error occurs or the actor is stopped
    private async Task<bool> ProcessMessages(bool isRestart)
    {
        if (isRestart)
            // ReSharper disable once ConvertClosureToMethodGroup
            RunHook(() => AfterRestart());

        try
        {
            await MessageLoopAsync();
        }
        catch (TaskCanceledException e)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            RunHook(() => BeforeStop());
            ((Actor) Parent)?.RemoveChild(Name);
            return true;
        }
        catch (Exception e)
        {
            RunHook(() => OnError(e, _currentlyProcessing.Message));
            
            var canRestart = await _restartPolicy.CanRestartAsync();
            if (!canRestart)
            {
                // ReSharper disable once ConvertClosureToMethodGroup
                RunHook(() => BeforeStop());
                ((Actor) Parent)?.RemoveChild(Name);
                return true;
            }
        }
        return false;
    }
    
    // process messages until an exception is thrown
    private async Task<Exception> MessageLoopAsync() 
    {
        try
        {
            while (true)
            {
                Sender = NullActor.Instance;
                _currentlyProcessing = null;
                _currentlyProcessing = await _mailbox.Reader.ReadAsync(_cancellationTokenSource.Token);
                Sender = _currentlyProcessing.Sender;
                
                // could throw, must be handled carefully
                await OnReceiveAsync(_currentlyProcessing.Message);
            }
        }
        catch (Exception e)
        {
            return e;
        }
    }
    #endregion
    
    #region Lifecycle and hooks
    public void Stop() => _cancellationTokenSource.Cancel();
    
    protected virtual void BeforeStop() {}
    protected virtual void OnError(Exception e, object message) {}
    protected virtual void AfterRestart() {}

    private void RunHook(Action hook)
    {
        try { hook(); }
        catch (Exception _) { }
    }
    #endregion
    
    #region Child-Actor creation
    public IActorRef ActorOf<T>(string name, params object[] args) where T : Actor => 
        ActorOf(name, typeof(T), args);
    
    internal IActorRef ActorOf(string name, Type actorType, params object[] args)
    {
        // build names with random part if they contain a "*"
        if (name.Contains('*'))
        {
            var randomPart = 
                Path.GetRandomFileName()
                    .Replace(".", "")
                    .Substring(0, 8);
            name = name.Replace("*", randomPart);
        }

        // args for the constructor (parent, name, ...args)
        var ctorArgs = args.Prepend(name).Prepend(Self).ToList();
        var ctorArgTypes = ctorArgs
            .Select(a => a.GetType())
            .ToArray();
        var ctor = actorType
            .GetConstructor(ctorArgTypes)
            ?? throw new ArgumentException($"No ctor in class {actorType.Name} found for provided arguments ({String.Join(", ", ctorArgTypes.Select(t => t.Name))})");
        
        var actor = ctor.Invoke(ctorArgs.ToArray()) as Actor;
        AddChild(actor.Name, actor);
        return actor;
    }
    #endregion

    #region Child management
    public IActorRef GetChild(string name) =>
        _children.ContainsKey(name)
            ? _children[name]
            : NullActor.Instance;
    
    private void AddChild(string name, Actor child)
    {
        if (!_children.TryAdd(name, child))
            throw new ArgumentException($"Actor name '{name}' is already present");
    }

    private void RemoveChild(string name) => 
        _children.Remove(name, out Actor value);

    #endregion

    public RouterBuilder WithRouter(IRoutingStrategy routingStrategy) =>
        new RouterBuilder(this, routingStrategy);
    
    public override string ToString() => $"ActorRef '{Name}'";
}
