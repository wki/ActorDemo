using System.Threading.Channels;

namespace MinimalActorLib;

/// <summary>
/// Base class for an actor
/// </summary>
public class Actor
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Channel<Envelope> _mailbox = Channel.CreateUnbounded<Envelope>();
    protected Actor? Parent;
    protected Actor? Sender;
    private Timer? _timer;

    private int _receiveTimeoutMs = 0;

    protected int ReceiveTimeoutMs
    {
        get => _receiveTimeoutMs;
        set { _receiveTimeoutMs = value; RestartTimer(); }
    }
    private object? _message;
    public ActorStatus ActorStatus { get; private set; } = ActorStatus.Initializing; 
    private Task _eventLoop;

    // TODO: maybe add "Name" properties
    
    private void Start()
    {
        _eventLoop = EventLoop();
    }
    
    private async Task EventLoop()
    {
        await OnStartAsync().IgnoreExceptions();

        while (true)
        {
            Sender = null;
            RestartTimer();
            ActorStatus = ActorStatus.Idle;

            try
            {
                (Sender, _message) = await _mailbox.Reader.ReadAsync(_cancellationTokenSource.Token);
                ActorStatus = ActorStatus.Processing;
                await OnReceiveAsync(_message);
            }
            catch (TaskCanceledException)
            {
                // will be raised when Stop() is called -> halt this Actor
                break;
            }
            catch (Exception ex)
            {
                // exception in user code occured and must be handled
                ActorStatus = ActorStatus.ErrorHandling;
                if (!await OnErrorAsync(_message, ex).IgnoreExceptions())
                    break;

                ActorStatus = ActorStatus.Restarting;
                await OnRestartAsync().IgnoreExceptions();
            }
        }

        _mailbox.Writer.Complete();
        _timer?.Dispose();
        _timer = null;
        
        ActorStatus = ActorStatus.Stopping;
        await OnStopAsync().IgnoreExceptions();
        ActorStatus = ActorStatus.Stopped;
        Tell(Parent, new Terminated(this));
        
        Parent = null;
        Sender = null;
        _message = null;
        _cancellationTokenSource.Dispose();
        _eventLoop.Dispose();
    }

    private void RestartTimer()
    {
        if (_timer is null)
        {
            if (_receiveTimeoutMs > 0)
                _timer = new Timer(
                    callback: _ =>
                    {
                        if (ActorStatus == ActorStatus.Idle)
                            Tell(this, TimeOut.Instance);
                    },
                    state: null,
                    dueTime: _receiveTimeoutMs,
                    period: -1); 
        }
        else
        {
            if (_receiveTimeoutMs <= 0)
            {
                _timer.Dispose();
                _timer = null;
            }
            else
            {
                _timer.Change(
                    dueTime: _receiveTimeoutMs,
                    period: -1);    
            }
        }
    }
    
    public void Stop() => _cancellationTokenSource.Cancel();
    
    public Actor ActorOf<T>(params object[] ctorArgs) where T : Actor => 
        ActorOf(typeof(T), ctorArgs);

    internal Actor ActorOf(Type actorType, params object[] ctorArgs)
    {
        var ctorArgTypes = ctorArgs
            .Select(a => a.GetType())
            .ToArray();
        var ctor = actorType
            .GetConstructor(ctorArgTypes)
            ?? throw new ArgumentException($"No ctor in class {actorType.Name} found for provided arguments ({string.Join(", ", ctorArgTypes.Select(t => t.Name))})");
        var actor = ctor.Invoke(ctorArgs.ToArray()) as Actor
            ?? throw new InvalidCastException($"class {actorType.Name} is not derived from Actor");
        actor.Parent = this;
        actor.Start();
        return actor;
    }
    
    public bool Tell(Actor? receiver, object message) =>
        SendMessage(this, receiver, message);

    public Task<T> AskAsync<T>(Actor receiver, object question, int timeOutMs = 500)
    {
        Console.WriteLine($"about to tell {question} to {receiver}");
        var answer = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        ActorOf<AskActor<T>>(receiver, question, answer, timeOutMs);
        return answer.Task;
    }
    
    protected bool Reply(object message) =>
        SendMessage(this, Sender, message);

    protected bool Forward(Actor? receiver) =>
        SendMessage(Sender!, receiver, _message!);
    
    private static bool SendMessage(Actor sender, Actor? receiver, object message) =>
        receiver is not null
        && receiver.ActorStatus < ActorStatus.Stopping
        && receiver._mailbox.Writer.TryWrite(new Envelope(sender, message));
    
    protected virtual Task OnStartAsync() =>
        Task.CompletedTask;
    
    protected virtual Task OnReceiveAsync(object message) =>
        Task.CompletedTask;

    protected virtual Task<bool> OnErrorAsync(object? message, Exception ex) =>
        Task.FromResult(false);

    protected virtual Task OnRestartAsync() =>
        Task.CompletedTask;
    
    protected virtual Task OnStopAsync() =>
        Task.CompletedTask;
    
    public override string ToString() =>
        $"[Actor {GetType().Name} {GetHashCode()}]";
}
