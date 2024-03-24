namespace HierarchicalJobRunner.Processing;

public interface IRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}
