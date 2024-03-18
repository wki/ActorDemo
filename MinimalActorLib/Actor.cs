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
    protected int ReceiveTimeoutMs { get; private set; } = 0;
    private object? _message;
    public ActorStatus ActorStatus { get; private set; } = ActorStatus.Initializing; 
    private readonly Task _eventLoop;

    // TODO: maybe add "Name" properties
    
    protected Actor()
    {
        _eventLoop = Task.Run(EventLoop);
    }
    
    private async Task EventLoop()
    {
        await CarefullyAsync(OnStartAsync);

        while (true)
        {
            try
            {
                Sender = null;
                ActorStatus = ActorStatus.Idle;
                RestartTimer();
                (Sender, _message) = await _mailbox.Reader.ReadAsync(_cancellationTokenSource.Token);
                ActorStatus = ActorStatus.Processing;
                await OnReceiveAsync(_message);
            }
            catch (TaskCanceledException _)
            {
                // will be raised when Stop() is called
                break;
            }
            catch (Exception ex)
            {
                
                ActorStatus = ActorStatus.ErrorHandling;
                if (!await CarefullyAsync(() => OnErrorAsync(_message, ex)))
                    break;

                ActorStatus = ActorStatus.Restarting;
                await CarefullyAsync(OnRestartAsync);
            }
        }

        _mailbox.Writer.Complete();
        _timer?.Dispose();
        _timer = null;
        
        ActorStatus = ActorStatus.Stopping;
        await CarefullyAsync(OnStopAsync);
        ActorStatus = ActorStatus.Stopped;
        Tell(Parent, new Terminated(this));
    }

    private Task<T> CarefullyAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            // do nothing
        }

        return Task.FromResult(default(T));
    }
    
    private Task CarefullyAsync(Func<Task> action)
    {
        try
        {
            return action();
        }
        catch (Exception)
        {
            // do nothing
        }
    
        return Task.CompletedTask;
    }
    
    protected void SetReceiveTimeout(int milliSeconds)
    {
        ReceiveTimeoutMs = milliSeconds;
        RestartTimer();
    }
    
    private void RestartTimer()
    {
        if (_timer is null)
        {
            if (ReceiveTimeoutMs > 0)
                _timer = new Timer(
                    callback: _ =>
                    {
                        if (ActorStatus == ActorStatus.Idle)
                            Tell(this, TimeOut.Instance);
                    },
                    state: null,
                    dueTime: ReceiveTimeoutMs,
                    period: -1); 
        }
        else
        {
            if (ReceiveTimeoutMs <= 0)
            {
                _timer.Dispose();
                _timer = null;
            }
            else
            {
                _timer.Change(ReceiveTimeoutMs, -1);    
            }
        }
    }
    
    public void Stop() => _cancellationTokenSource.Cancel();
    
    public Actor ActorOf<T>(params object[] ctorArgs) where T : Actor => 
        ActorOf(typeof(T), ctorArgs);

    public Actor ActorOf(Type actorType, params object[] ctorArgs)
    {
        var ctorArgTypes = ctorArgs
            .Select(a => a.GetType())
            .ToArray();
        var ctor = actorType
            .GetConstructor(ctorArgTypes)
            ?? throw new ArgumentException($"No ctor in class {actorType.Name} found for provided arguments ({String.Join(", ", ctorArgTypes.Select(t => t.Name))})");
        
        var actor = ctor.Invoke(ctorArgs.ToArray()) as Actor;
        actor.Parent = this;
        return actor;
    }
    
    public bool Tell(Actor receiver, object message) => 
        SendMessage(this, receiver, message);

    public Task<T> AskAsync<T>(Actor receiver, object question, int timeOutMs = 500)
    {
        var answer = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        new AskActor<T>(receiver, question, answer, timeOutMs);
        return answer.Task;
    }
    
    protected bool Reply(object message) => 
        SendMessage(this, Sender, message);

    protected bool Forward(Actor receiver) => 
        SendMessage(Sender, receiver, _message);
    
    private bool SendMessage(Actor sender, Actor receiver, object message) =>
        receiver is not null
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
