using ActorLib;

namespace ActorWebDemo.Service;

public class Echo : Actor
{
    private int _count = 0;

    protected override void OnReceive(object message)
    {
        var echo = $"Echo: {message} ({++_count})";
        _logger.LogInformation($"Received: {message}, replying: {echo}");
        Reply(echo);
    }
}
