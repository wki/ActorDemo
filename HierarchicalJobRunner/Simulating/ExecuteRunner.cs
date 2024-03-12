using HierarchicalJobRunner.Job;
using HierarchicalJobRunner.Processing;

namespace HierarchicalJobRunner.Simulating;

public class ExecuteRunner: IRunner
{
    private readonly ExecuteJob _executeJob;

    public ExecuteRunner(ExecuteJob executeJob)
    {
        _executeJob = executeJob;
    }
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);
    }
}
