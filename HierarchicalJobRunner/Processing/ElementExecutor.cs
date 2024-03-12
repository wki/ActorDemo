using HierarchicalJobRunner.Job;
using HierarchicalJobRunner.Simulating;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class ElementExecutor: Actor
{
    private readonly Element _element;
    private readonly Actor _parent;
    private readonly IRunner _runner;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _task;

    public ElementExecutor(Element element, Actor parent)
    {
        _element = element;
        _parent = parent;
        _cancellationTokenSource = new CancellationTokenSource();
        _task = null;

        // FIXME make more dynamic and configurable for DryRun / Actual
        _runner = element switch
        {
            ExecuteJob executeJob => new Simulating.ExecuteRunner(executeJob),
            DownloadJob downloadJob => new Simulating.DownloadRunner(downloadJob),
            _ => throw new NoRunnerFoundException("")
        };
    }

    protected override Task OnReceive(object message)
    {
        Console.WriteLine($"{this}: {_element.GetType().Name} '{_element.Name}' received {message.GetType().Name} from {Sender}");

        switch (message)
        {
            case Start:
                _task = _runner.RunAsync(_cancellationTokenSource.Token);
                _task
                    .WaitAsync(TimeSpan.FromSeconds(42))
                    .ContinueWith(t =>
                {
                    if (t.IsCanceled)
                        Tell(_parent, new ChildCanceled(_element.Id));
                    else if (t.IsFaulted && t.Exception.InnerExceptions.Any(e => e is TimeoutException))
                        Tell(_parent, new ChildTimedOut(_element.Id));
                    else if (t.IsFaulted)
                        Tell(_parent, new ChildFailed(_element.Id));
                    else
                        Tell(_parent, new ChildCompleted(_element.Id));

                    Stop();
                });
                
                break;
            case Cancel:
                _cancellationTokenSource.Cancel();
                break;
        }

        return Task.CompletedTask;
    }
}