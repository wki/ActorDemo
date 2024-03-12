namespace HierarchicalJobRunner.Job;

public class DownloadJob : Element
{
    public Uri Url { get; set; }
    public string DestinationPath { get; set; }
}