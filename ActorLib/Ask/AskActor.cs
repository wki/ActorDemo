namespace ActorLib;

public class AskActor<T>: Actor
{
    private readonly Actor _receiver;
    private readonly object _question;
    private readonly int _timeoutMs;
    private readonly TaskCompletionSource<T> _taskCompletionSource;

    public AskActor(Actor receiver, object question, TaskCompletionSource<T> taskCompletionSource, int timeoutMs)
    {
        _receiver = receiver;
        _question = question;
        _timeoutMs = timeoutMs;
        _taskCompletionSource = taskCompletionSource;
        SetReceiveTimeout(timeoutMs);
    }

    protected override void BeforeStart()
    {
        Tell(_receiver, _question);
    }

    protected override void OnReceive(object message)
    {
        if (message is T answer)
        {
            _taskCompletionSource.TrySetResult(answer);
            Stop();
        }
        else if (message is TimeOut)
        {
            _taskCompletionSource.TrySetException(new AskTimeoutException($"no answer from {_receiver} on {_question} within {_timeoutMs:0}ms"));
            Stop();
        }
    }
}