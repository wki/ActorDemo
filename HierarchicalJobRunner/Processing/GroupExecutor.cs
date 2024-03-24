using HierarchicalJobRunner.Job;
using MinimalActorLib;

namespace HierarchicalJobRunner.Processing;

public class GroupExecutor: Actor
{
    private Actor _startingActor;
    private readonly Group _group;
    private readonly List<Actor> _executors;
    private readonly Dictionary<Guid, RunStatus> _runStatusFor;
    private int NrStarted => _runStatusFor.Values.Count(r => r > RunStatus.Idle);
    private int NrCompleted => _runStatusFor.Values.Count(r => r == RunStatus.Completed);
    private int NrCanceled => _runStatusFor.Values.Count(r => r == RunStatus.Canceled);
    private int NrFailed => _runStatusFor.Values.Count(r => r == RunStatus.Failed);

    public GroupExecutor(Group group)
    {
        _group = group;
        _executors = group.Children
            .Select(child => 
                child is Group node
                    ? ActorOf<GroupExecutor>(node)
                    : ActorOf<ElementExecutor>(child))
            .ToList();
        _runStatusFor = group.Children
            .ToDictionary(
                keySelector: c => c.Id,
                elementSelector: c => RunStatus.Idle);
    }

    protected override Task OnReceiveAsync(object message)
    {
        Console.WriteLine($"{this}: {_group.GetType().Name} '{_group.Name}' received {message.GetType().Name} from {Sender}");

        switch (message)
        {
            case Start:
                _startingActor = Sender;
                // start first child (TODO: all children when parallel)
                if (_executors.Any())
                    Tell(_executors.First(), new Start());
                break;
            
            case Cancel cancel:
            case Retry retry:
            case Skip skip:
                // let the child decide if operation is interesting...
                foreach (var child in _executors)
                    Forward(child);
                break;
            
            case ChildStarted started:
                _runStatusFor[started.Id] = RunStatus.Running;
                // in case of a retry _nrStarted could be > _node.Children.Count
                if (NrStarted == 0)
                    Tell(Parent, new ChildStarted(_group.Id));
                break;
            
            case ChildFailed failed:
                _runStatusFor[failed.Id] = RunStatus.Failed;
                if (NrFailed == 1)
                {
                    Tell(Parent, new ChildFailed(_group.Id));

                    Tell(_startingActor, new Finished(RunStatus.Failed));
                    Console.WriteLine("*** FAILED");
                }
                break;

            case ChildSkipped skipped:
                _runStatusFor[skipped.Id] = RunStatus.Skipped;
                var index = _group.Children.FindIndex(c => c.Id == skipped.Id);
                if (index >= 0)
                {
                    // either start next child or tell parent that we are complete
                    if (index <= _group.Children.Count)
                        Tell(_executors[index + 1], new Start());
                    else
                        Tell(Parent, new ChildCompleted(_group.Id));
                }
                break;
            
            case ChildCompleted completed:
                _runStatusFor[completed.Id] = RunStatus.Completed;
                if (NrCompleted < _group.Children.Count)
                    Tell(_executors.Skip(NrCompleted).First(), new Start());
                else if (NrCompleted == _group.Children.Count)
                {
                    Tell(Parent, new ChildCompleted(_group.Id));
                    Tell(_startingActor, new Finished(RunStatus.Completed));
                    Console.WriteLine("*** COMPLETED");
                }

                break;
            
            case ChildCanceled canceled:
                _runStatusFor[canceled.Id] = RunStatus.Canceled;
                if (NrCanceled == 1)
                {
                    Tell(Parent, new ChildCanceled(_group.Id));

                    Console.WriteLine("*** CANCELED");
                }
                break;
        }

        return Task.CompletedTask;
    }
}