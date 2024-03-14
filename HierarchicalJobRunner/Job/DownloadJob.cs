namespace HierarchicalJobRunner.Job;

public class DownloadJob : Element, IWithTimeout
{
    public Uri Url { get; set; }
    public string DestinationPath { get; set; }
    
    public int TimeoutMs { get; set; }
}