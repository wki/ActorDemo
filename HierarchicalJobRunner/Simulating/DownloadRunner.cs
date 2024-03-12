using HierarchicalJobRunner.Job;
using HierarchicalJobRunner.Processing;

namespace HierarchicalJobRunner.Simulating;

public class DownloadRunner: IRunner
{
    private readonly DownloadJob _downloadJob;

    public DownloadRunner(DownloadJob downloadJob)
    {
        _downloadJob = downloadJob;
    }
    
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken);
    }
}