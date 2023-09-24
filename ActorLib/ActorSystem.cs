using Microsoft.Extensions.Logging;

namespace ActorLib;

public class ActorSystem : Actor
{
    public ActorSystem(string name, ILogger logger = null)
    {
        Name = name;
        if (logger is not null)
            _logger = logger;
        Start();
    }

    protected override void OnReceive(object message)
    {
        if (message is ChildTerminated childTerminated)
        {
            _logger.LogDebug($"Child Terminated: {childTerminated.Name}");
        }
    }
}