namespace ActorDemo.Ask;

// ReSharper disable once ClassNeverInstantiated.Global
public class AskActor<T>: Actor
{
    private readonly IActorRef _receiver;
    private readonly object _message;
    private readonly TaskCompletionSource<T> _taskCompletionSource;
    private readonly int _timeoutMillis;
    private readonly Timer _timer;

    public AskActor(IActorRef receiver, object message, TaskCompletionSource<T> taskCompletionSource, int timeoutMillis)
    {
        _receiver = receiver;
        _message = message;
        _taskCompletionSource = taskCompletionSource;
        _timeoutMillis = timeoutMillis;
        
        _timer = new Timer(TimeOver, null, timeoutMillis, 100_000);
    }

    public override void AfterStart()
    {
        Tell(_receiver, _message);
    }

    private void TimeOver(object? o)
    {
        _taskCompletionSource.SetException(new AskTimeoutException($"no answer from {_receiver} on {_message} within {_timeoutMillis:0}ms"));
    }
    
    public override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case T answer:
                _taskCompletionSource.SetResult(answer);
                _timer.Dispose();
                break;
            default:
                Console.WriteLine($"{Name}: received invalid answer {message}");
                break;
        }
        
        return Task.CompletedTask;
    }
}