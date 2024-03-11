using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class ExecuteRunner : Actor
{
    private readonly ExecuteJob _executeJob;
    private readonly Actor _parent;

    public ExecuteRunner(ExecuteJob executeJob, Actor parent)
    {
        _executeJob = executeJob;
        _parent = parent;
    }

    protected override async Task OnReceive(object message)
    {
        Console.WriteLine($"{this}: '{_executeJob.Name}' received {message.GetType().Name}");

        if (message is Start)
        {
            Tell(_parent, new Started(_executeJob.Id));
            await Task.Delay(2000);
            Tell(_parent, new Completed(_executeJob.Id));
        }
    }
}