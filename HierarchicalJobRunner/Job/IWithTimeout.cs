namespace HierarchicalJobRunner.Job;

public interface IWithTimeout
{
    int TimeoutMs { get; }
}