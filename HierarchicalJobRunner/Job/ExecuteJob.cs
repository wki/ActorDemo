namespace HierarchicalJobRunner.Job;

public class ExecuteJob : Element, IWithTimeout
{
    public string CommandLine { get; set; }

    public int TimeoutMs { get; set; }
}