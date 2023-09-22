namespace ActorLib.Restart;

public interface IRestartPolicy
{
    public Task<bool> CanRestartAsync();
}