namespace ActorLib;

public interface IRestartPolicy
{
    Task<bool> CanRestartAsync();
}