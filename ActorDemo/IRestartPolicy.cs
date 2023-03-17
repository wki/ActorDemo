namespace ActorDemo;

public interface IRestartPolicy
{
    Task<bool> CanRestartAsync();
}