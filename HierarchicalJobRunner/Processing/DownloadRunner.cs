using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class DownloadRunner : Actor
{
    private readonly DownloadJob _downloadJob;
    private readonly Actor _parent;

    public DownloadRunner(DownloadJob downloadJob, Actor parent)
    {
        _downloadJob = downloadJob;
        _parent = parent;
    }

    protected override async Task OnReceive(object message)
    {
        Console.WriteLine($"{this}: '{_downloadJob.Name}' received {message.GetType().Name}");

        if (message is Start)
        {
            Tell(_parent, new Started(_downloadJob.Id));
            await Task.Delay(1000);
            Tell(_parent, new Completed(_downloadJob.Id));
        }
    }
}