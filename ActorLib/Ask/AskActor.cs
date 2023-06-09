namespace ActorLib.Ask;

// ReSharper disable once ClassNeverInstantiated.Global

/// <summary>
/// temporary actor sending one message to a receiver waiting for an answer with timeout
/// </summary>
/// <typeparam name="T"></typeparam>
public class AskActor<T>: Actor
{
    private readonly IActorRef _receiver;
    private readonly object _question;
    private readonly TaskCompletionSource<T> _taskCompletionSource;
    private readonly int _timeoutMillis;
    private readonly Timer _timer;

    public AskActor(IActorRef receiver, object question, TaskCompletionSource<T> taskCompletionSource, int timeoutMillis)
    {
        _receiver = receiver;
        _question = question;
        _taskCompletionSource = taskCompletionSource;
        _timeoutMillis = timeoutMillis;
        
        _timer = new Timer(TimeOver, null, timeoutMillis, 100_000);
    }

    public override void AfterStart()
    {
        Tell(_receiver, _question);
    }

    private void TimeOver(object? _)
    {
        _taskCompletionSource.SetException(new AskTimeoutException($"no answer from {_receiver} on {_question} within {_timeoutMillis:0}ms")); 
        _timer.Dispose();
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