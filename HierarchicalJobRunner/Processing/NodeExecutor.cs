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
        Console.WriteLine($"{this}: {_node.GetType().Name} '{_node.Name}' received {message.GetType().Name} from {Sender}");

        switch (message)
        {
            case Start:
                // start first child (TODO: all children when parallel)
                if (_executors.Any())
                    Tell(_executors.First(), new Start());
                break;
            
            case ChildStarted:
                if (_nrStarted++ == 0 && _parent is not null)
                    Tell(_parent, new ChildStarted(_node.Id));
                break;
            
            case ChildCompleted:
                ++_nrCompleted;
                if (_nrCompleted < _node.Children.Count)
                    Tell(_executors.Skip(_nrCompleted).First(), new Start());
                else if (_nrCompleted == _node.Children.Count && _parent is not null)
                    Tell(_parent, new ChildCompleted(_node.Id));
                else if (_nrCompleted == _node.Children.Count)
                    Console.WriteLine("*** COMPLETED");
                break;
            
            case ChildFailed:
                if (++_nrFailed == 1)
                {
                    if (_parent is not null)
                        Tell(_parent, new ChildFailed(_node.Id));

                    Console.WriteLine("*** FAILED");
                }

                break;
        }

        return Task.CompletedTask;
    }
}