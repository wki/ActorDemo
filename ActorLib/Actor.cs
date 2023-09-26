using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using ActorLib.Restart;
using ActorLib.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MessageHandler = System.Func<object, System.Threading.Tasks.Task>;

namespace ActorLib;

public abstract class Actor
{
    // ReSharper disable ConvertClosureToMethodGroup
    
    public string Name { get; internal set; } = "still_unnamed";
    protected Actor Parent { get; private set; }
    protected Actor Self => this;
    protected internal ImmutableList<Actor> Children => _children.Values.ToImmutableList();
    protected Actor Sender { get; private set; } = NullActor.Instance;
    public ActorStatus ActorStatus { get; private set; } = ActorStatus.Initializing;
    private readonly ConcurrentDictionary<string, Actor> _children = new();
    private readonly Channel<Envelope> _mailbox = Channel.CreateUnbounded<Envelope>();
    private Envelope? _currentlyProcessing;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly IRestartPolicy _restartPolicy = new DelayedRestartPolicy();
    private readonly Queue<Envelope> _stash = new();
    private Task _messageProcessingTask;
    private MessageHandler _messageHandlerAsync;
    private readonly Stack<MessageHandler> _stackedMessageHandlers = new();
    protected internal ILogger _logger = NullLogger.Instance;
    private Timer? _timer;
    protected int ReceiveTimeoutMs { get; private set; }

    protected Actor()
    {
        _messageHandlerAsync = m => OnReceiveAsync(m);
    }
    
    public string ActorPath => String.Join('/', GetPath());

    private IEnumerable<String> GetPath() =>
        Parent is null ? new[] { Name } : Parent.GetPath().Append(Name);
    
    #region Message Processing
    protected virtual void OnReceive(object message) {}
    
    protected virtual Task OnReceiveAsync(object message)
    {
        OnReceive(message);
        return Task.CompletedTask;
    }

    protected void SetReceiveTimeout(int milliSeconds)
    {
        ReceiveTimeoutMs = milliSeconds;
        RestartTimer();
    }

    private void RestartTimer()
    {
        if (_timer is null && ReceiveTimeoutMs <= 0) return;
        
        _timer?.Dispose();
        _timer = ReceiveTimeoutMs <= 0 
            ? null 
            : new Timer(_ => Tell(this, TimeOut.Instance), null, ReceiveTimeoutMs, -1);
    }

    public void Tell(Actor receiver, object message) =>
        EnqueueEnvelope(this, receiver, message);

    protected void Reply(object message) =>
        EnqueueEnvelope(this, Sender, message);

    protected void Forward(Actor receiver) =>
        EnqueueEnvelope(Sender, receiver, _currentlyProcessing.Message);

    public Task<T> Ask<T>(Actor receiver, object question, int timeOutMillis = 500)
    {
        var answer = new TaskCompletionSource<T>();
        ActorOf<AskActor<T>>("ask-*", receiver, question, answer, timeOutMillis);
        return answer.Task;
    }
    
    private void EnqueueEnvelope(Actor sender, Actor receiver, object message) =>
        EnqueueEnvelope(new Envelope(sender, receiver, message));
    
    private void EnqueueEnvelope(Envelope envelope)
    {
        // FIXME: exception when write fails?
        if (!envelope.Receiver._mailbox.Writer.TryWrite(envelope))
            _logger.LogError($"{this}: could not enqueue Message {envelope.Message} from {envelope.Sender}");
    }

    // process all messages in the entire life of the actor and handle end of life
    private async Task RunMessageLoopAsync()
    {
        RunHook(() => BeforeStart());

        while (await ProcessMessagesAsync())
        {
            ActorStatus = ActorStatus.Restarting;
            RunHook(() => AfterRestart());
        }

        ActorStatus = ActorStatus.Stopping;
        RunHook(() => AfterStop());
        Children.ForEach(c => c.Stop());
        Parent?.RemoveChild(Name);

        if (Parent is not null && Parent.ActorStatus <= ActorStatus.Stopping)
        {
            EnqueueEnvelope(
                sender: NullActor.Instance,
                receiver: Parent,
                message: new ChildTerminated(Name)
            );
        }
        _messageProcessingTask.Dispose();
        _messageProcessingTask = null;
        ActorStatus = ActorStatus.Stopped;
    }
    
    // process messages until an error occurs or the actor is stopped
    // returns true if actor can restart
    private async Task<bool> ProcessMessagesAsync()
    {
        try
        {
            while (true)
            {
                ActorStatus = ActorStatus.Idle;
                Sender = NullActor.Instance;
                _currentlyProcessing = null;

                _currentlyProcessing = await _mailbox.Reader.ReadAsync(_cancellationTokenSource.Token);
                Sender = _currentlyProcessing.Sender;
                
                RestartTimer();

                ActorStatus = ActorStatus.Processing;
                await _messageHandlerAsync(_currentlyProcessing.Message);
            }
        }
        catch (Exception e)
        {
            if (e is TaskCanceledException) return false; // stopped --> no restart allowed

            _logger.LogError($"{this}: Exception {e.GetType().Name} occured: {e.Message}");
            ActorStatus = ActorStatus.ErrorHandling;
            RunHook(() => OnError(e, _currentlyProcessing.Message));
        }

        return await _restartPolicy.CanRestartAsync();
    }
    #endregion
    
    #region Lifecycle and hooks
    internal void Start() => _messageProcessingTask = RunMessageLoopAsync();

    public void Stop() => _cancellationTokenSource.Cancel();

    protected virtual void BeforeStart() {}
    protected virtual void OnError(Exception e, object message) {}
    protected virtual void AfterRestart() {}
    protected virtual void AfterStop() {}

    private void RunHook(Action hook)
    {
        try { hook(); }
        catch (Exception _) { }
    }
    #endregion
    
    #region Child-Actor creation
    public Actor ActorOf<T>(string name, params object[] ctorArgs) where T : Actor => 
        ActorOf(name, typeof(T), ctorArgs);
    
    internal Actor ActorOf(string name, Type actorType, params object[] ctorArgs)
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

        // TODO: choose first constructor and inject dependency-injectable objects
        var ctorArgTypes = ctorArgs
            .Select(a => a.GetType())
            .ToArray();
        var ctor = actorType
            .GetConstructor(ctorArgTypes)
            ?? throw new ArgumentException($"No ctor in class {actorType.Name} found for provided arguments ({String.Join(", ", ctorArgTypes.Select(t => t.Name))})");
        
        var actor = ctor.Invoke(ctorArgs.ToArray()) as Actor;
        actor.Name = name;
        actor.Parent = this;
        AddChild(actor.Name, actor);
        actor._logger = _logger;
        actor.Start();
        return actor;
    }
    #endregion

    #region Child management
    public Actor GetChild(string name) =>
        _children.ContainsKey(name) ? _children[name] : NullActor.Instance;
    
    private void AddChild(string name, Actor child)
    {
        if (!_children.TryAdd(name, child))
            throw new ArgumentException($"Actor name '{name}' is already present");
    }

    private void RemoveChild(string name) => 
        _children.Remove(name, out var _);

    public IEnumerable<string> AllChildPaths() =>
        Children.SelectMany(c => 
            c.Children.Select(cc => cc.ActorPath).Prepend(c.ActorPath)
        );
    #endregion
    
    #region Stash Management
    protected void Stash() => _stash.Enqueue(_currentlyProcessing);

    protected void UnStashAll()
    {
        while (_stash.Any())
            EnqueueEnvelope(_stash.Dequeue());
    }
    #endregion
    
    #region State Handling
    protected void Become(MessageHandler handleAsync) => 
        _messageHandlerAsync = handleAsync;

    protected void BecomeStacked(MessageHandler handleAsync)
    {
        _stackedMessageHandlers.Push(_messageHandlerAsync);
        Become(handleAsync);
    }

    protected void UnBecomeStacked() => 
        _messageHandlerAsync = _stackedMessageHandlers.Pop();
    #endregion

    public RouterBuilder WithRouter(IRoutingStrategy routingStrategy) =>
        new RouterBuilder(this, routingStrategy);
    
    public override string ToString() => $"Actor '{Name}'";
}
