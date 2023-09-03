namespace ActorSimpleLib;

public interface IRestartPolicy
{
    public Task<bool> CanRestartAsync();
}