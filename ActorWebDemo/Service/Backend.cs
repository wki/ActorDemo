using ActorLib;

namespace ActorWebDemo.Service;

public class Backend: IHostedService
{
    private ActorSystem _system;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _system = new ActorSystem("backend");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}