using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class Processor
{
    private readonly Element _element;
    private readonly Actor _executor;

    public Processor(Element element)
    {
        _element = element;
        _executor = Executor.For(_element);
        _executor.Tell(_executor, new Start());
    }
}