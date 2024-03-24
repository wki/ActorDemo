using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class ElementExecutor: Actor
{
    
    private readonly Element _element;
    private readonly IRunner _runner;
    private CancellationTokenSource _cancellationTokenSource;
    private Task? _task;
    private RunStatus _runStatus;

    public ElementExecutor(Element element)
    {
        _element = element;
        _task = null;
        _runStatus = RunStatus.Idle;

        // FIXME make more dynamic and configurable for DryRun / Actual
        _runner = element switch
        {
            ExecuteJob executeJob => new Simulating.ExecuteRunner(executeJob),
            DownloadJob downloadJob => new Simulating.DownloadRunner(downloadJob),
            _ => throw new NoRunnerFoundException("")
        };
    }

    protected override Task OnReceiveAsync(object message)
    {
        Console.WriteLine($"{this}: {_element.GetType().Name} '{_element.Name}' received {message.GetType().Name} from {Sender}");

        switch (message)
        {
            case Start:
            case Retry retry when retry.Id == _element.Id && _runStatus > RunStatus.Skipped:
                _cancellationTokenSource = new CancellationTokenSource();
                if (_element is IWithTimeout timeoutElement && timeoutElement.TimeoutMs > 0)
                    _cancellationTokenSource.CancelAfter(timeoutElement.TimeoutMs);
                _task = _runner.RunAsync(_cancellationTokenSource.Token);
                _task.ContinueWith(OnTaskCompleted);
                _runStatus = RunStatus.Running;
                Tell(Parent, new ChildStarted(_element.Id));
                break;
            case Cancel cancel when cancel.Id == _element.Id && _runStatus == RunStatus.Running:
                _cancellationTokenSource?.Cancel();
                break;
            case Skip skip when skip.Id == _element.Id && _runStatus > RunStatus.Skipped:
                _runStatus = RunStatus.Skipped;
                Tell(Parent, new ChildSkipped(_element.Id));                
                break;
        }

        return Task.CompletedTask;
    }

    private void OnTaskCompleted(Task t)
    {
        Console.WriteLine($"Task completed with status: {t.Status}");
        switch (t.Status)
        {
            case TaskStatus.Canceled:
                _runStatus = RunStatus.Canceled;
                Tell(Parent, new ChildCanceled(_element.Id));
                break;
            case TaskStatus.Faulted:
                _runStatus = RunStatus.Failed;
                Tell(Parent, new ChildFailed(_element.Id));
                break;
            default:
                _runStatus = RunStatus.Completed;
                Tell(Parent, new ChildCompleted(_element.Id));
                break;
        }
    }
}