using System.ComponentModel.DataAnnotations;

namespace HierarchicalJobRunner.Job;

public abstract class Node: Element
{
    public List<Element> Children { get; set; }
    public ProcessingOrder ProcessingOrder { get; set; }
}