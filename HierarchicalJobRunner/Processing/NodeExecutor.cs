using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class NodeExecutor: Actor
{
    private readonly Node _node;
    private readonly Actor _parent;
    private readonly List<Actor> _executors;
    private int _nrStarted = 0;
    private int _nrCompleted = 0;
    private int _nrFailed = 0;

    public NodeExecutor(Node node, Actor parent)
    {
        _node = node;
        _parent = parent;
        _executors = node.Children
            .Select(child => Executor.For(child, this))
            .ToList();
    }

    protected override Task OnReceive(object message)
    {
        Console.WriteLine($"{this}: {_node.GetType().Name} '{_node.Name}' received {message.GetType().Name}");

        switch (message)
        {
            case Start:
                // start first child (TODO: all children when parallel)
                if (_executors.Any())
                    Tell(_executors.First(), new Start());
                break;
            
            case Started:
                if (_nrStarted++ == 0)
                    Tell(_parent, new Started(_node.Id));
                break;
            
            case Completed:
                ++_nrCompleted;
                if (_nrCompleted < _node.Children.Count)
                    Tell(_executors.Skip(_nrCompleted).First(), new Start());
                else if (_nrCompleted == _node.Children.Count)
                    Tell(_parent, new Completed(_node.Id));
                break;
            
            case Failed:
                if (++_nrFailed == 1)
                    Tell(_parent, new Failed(_node.Id));
                break;
        }

        return Task.CompletedTask;
    }
}