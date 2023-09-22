namespace ActorSimpleLib.Restart;

public interface IRestartPolicy
{
    public Task<bool> CanRestartAsync();
}