using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public static class Executor
{
    public static Actor For(Element element, Actor parent = null) =>
        element is Node node
            ? new NodeExecutor(node, parent)
            : new ElementExecutor(element, parent);
}