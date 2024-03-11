using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class ElementExecutor: Actor
{
    private readonly Element _element;
    private readonly Actor _parent;
    private readonly Actor _runner;

    public ElementExecutor(Element element, Actor parent)
    {
        _element = element;
        _parent = parent;
        
        // FIXME make more dynamic and configurable for DryRun / Actual
        _runner = element is ExecuteJob executeJob
            ? new ExecuteRunner(executeJob, this)
            : element is DownloadJob downloadJob
                ? new DownloadRunner(downloadJob, this)
                : throw new NoRunnerFoundException("");
    }

    protected override Task OnReceive(object message)
    {
        Console.WriteLine($"{this}: {_element.GetType().Name} '{_element.Name}' received {message.GetType().Name}");
        
        switch (message)
        {
            case Start:
                Tell(_runner, message);
                break;
            case Started:
            case Completed:
            case Failed:
                Console.WriteLine($"should tell {_parent}");
                Tell(_parent, message);
                break;
        }

        return Task.CompletedTask;
    }
}