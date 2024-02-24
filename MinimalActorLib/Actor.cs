using System.Threading.Channels;

namespace MinimalActorLib;

/// <summary>
/// Base class for an actor
/// </summary>
public class Actor
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Channel<Envelope> _mailbox = Channel.CreateUnbounded<Envelope>();
    protected Actor? Sender;
    private Timer? _timer;
    protected int ReceiveTimeoutMs { get; private set; } = 0;
    private object? _message;
    public ActorStatus ActorStatus { get; private set; } = ActorStatus.Initializing; 
    private readonly Task _eventLoop;

    public Actor()
    {
        _eventLoop = Task.Run(EventLoop);
    }
    
    private async Task EventLoop()
    {
        await OnStart();

        while (true)
        {
            try
            {
                Sender = null;
                ActorStatus = ActorStatus.Idle;
                (Sender, _message) = await _mailbox.Reader.ReadAsync(_cancellationTokenSource.Token);
                ActorStatus = ActorStatus.Processing;
                RestartTimer();
                await OnReceive(_message);
            }
            catch (TaskCanceledException _)
            {
                break;
            }
            catch (Exception ex)
            {
                ActorStatus = ActorStatus.ErrorHandling;
                if (!await OnError(_message, ex))
                    break;

                ActorStatus = ActorStatus.Restarting;
                await OnRestart();
            }
        }

        ActorStatus = ActorStatus.Stopping;
        await OnStop();
        ActorStatus = ActorStatus.Stopped;
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

    public void Stop() =>
        _cancellationTokenSource.Cancel();
    
    public bool Tell(Actor receiver, object message) =>
        SendMessage(this, receiver, message);

    protected Task<T> Ask<T>(Actor receiver, object question, int timeOutMs = 500)
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
        receiver._mailbox.Writer.TryWrite(new Envelope(sender, message));

    protected virtual Task OnStart() =>
        Task.CompletedTask;
    
    protected virtual Task OnReceive(object message) => 
        Task.CompletedTask;

    protected virtual Task<bool> OnError(object? message, Exception ex) => 
        Task.FromResult(false);

    protected virtual Task OnRestart() =>
        Task.CompletedTask;
    
    protected virtual Task OnStop() => 
        Task.CompletedTask;

    public override string ToString() =>
        $"[{GetType().Name} {GetHashCode()}]";
}
