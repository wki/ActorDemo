using ActorLib;

namespace ActorWebDemo.Service;

public class Backend: IHostedService
{
    private readonly ILogger<Backend> _logger;
    private ActorSystem _system;
    private Actor _echo;

    public Backend(ILogger<Backend> logger)
    {
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("starting Backend Service...");
        _system = new ActorSystem("backend", _logger);
        _echo = _system.ActorOf<Echo>("echo");
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("stopping Backend Service...");
        return Task.CompletedTask;
    }

    public Task DoSomething()
    {
        _logger.LogInformation("Do Something...");
        return Task.CompletedTask;
    }

    public Task<string> Echo(object message) =>
        _system.Ask<string>(_echo, message);

    public IEnumerable<string> ActorPaths() =>
        _system.AllChildPaths();
}