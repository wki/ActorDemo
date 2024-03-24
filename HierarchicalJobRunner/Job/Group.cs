namespace HierarchicalJobRunner.Job;

public class Group: Element
{
    public List<Element> Children { get; set; }
    public ProcessingOrder ProcessingOrder { get; set; }
}