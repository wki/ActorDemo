namespace MinimalActorLib;

/// <summary>
/// A short lived actor for asking a question to an other actor and waiting for the answer or a timeout
/// </summary>
/// <typeparam name="T">type of the answer expected</typeparam>
internal class AskActor<T>: Actor
{
    private readonly Actor _receiver;
    private readonly object _question;
    private readonly TaskCompletionSource<T> _taskCompletionSource;
    private readonly int _timeoutMs;

    public AskActor(Actor receiver, object question, TaskCompletionSource<T> taskCompletionSource, int timeoutMs)
    {
        Console.WriteLine($"Instantiate AskActor with receiver={receiver}");
        _receiver = receiver;
        _question = question;
        _taskCompletionSource = taskCompletionSource;
        _timeoutMs = timeoutMs;
        ReceiveTimeoutMs = timeoutMs;
    }

    protected override Task OnStartAsync()
    {
        Tell(_receiver, _question);
        return Task.CompletedTask;
    }

    protected override Task OnReceiveAsync(object message)
    {
        switch (message)
        {
            case T answer:
                _taskCompletionSource.TrySetResult(answer);
                Stop();
                break;
            case TimeOut:
                _taskCompletionSource.TrySetException(new AskTimeoutException($"no answer from {_receiver} on {_question} within {_timeoutMs:0}ms"));
                Stop();
                break;
        }

        return Task.CompletedTask;
    }

    protected override Task<bool> OnErrorAsync(object? message, Exception ex)
    {
        // last chance. try to forward the exception we received, although unlikely
        _taskCompletionSource.TrySetException(ex);
        return Task.FromResult(false);
    }
}