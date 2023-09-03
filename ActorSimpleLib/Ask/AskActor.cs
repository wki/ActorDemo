namespace ActorSimpleLib;

public class AskActor<T>: Actor
{
    private readonly IActorRef _receiver;
    private readonly object _question;
    private readonly TaskCompletionSource<T> _taskCompletionSource;
    private readonly int _timeoutMillis;
    private readonly Timer _timer;
    private bool _answered;

    public AskActor(IActorRef parent, string name, IActorRef receiver, object question, TaskCompletionSource<T> taskCompletionSource, int timeoutMillis)
        :base(parent, name)
    {
        _receiver = receiver;
        _question = question;
        _taskCompletionSource = taskCompletionSource;
        _timeoutMillis = timeoutMillis;
        
        _timer = new Timer(TimeOver, null, timeoutMillis, 100_000);
        _answered = false;
        Tell(_receiver, _question);
    }

    private void TimeOver(object? _)
    {
        if (_answered) return;
        
        _taskCompletionSource.TrySetException(new AskTimeoutException($"no answer from {_receiver} on {_question} within {_timeoutMillis:0}ms")); 
        // _timer.Dispose();
        _answered = true;
        // Stop();
    }
    
    protected override Task OnReceiveAsync(object message)
    {
        if (_answered) return Task.CompletedTask;
        
        Console.WriteLine($"{this} - received: {message}");
        if (message is T answer)
        {
            _taskCompletionSource.TrySetResult(answer);
            // _timer.Dispose();
            _answered = true;
            // Stop();
        }

        return Task.CompletedTask;
    }
}