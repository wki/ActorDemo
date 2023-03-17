namespace ActorDemo;

/// <summary>
/// Restart an actor after failure up to a given count and wait before restarting
/// </summary>
public class DelayedRestartPolicy: IRestartPolicy
{
    private readonly int _maxRestarts;
    private readonly int _delayMillis;
    private int _nrRestarts = 0;
    
    public DelayedRestartPolicy(int maxRestarts = 10, int delayMillis = 200)
    {
        _maxRestarts = maxRestarts;
        _delayMillis = delayMillis;
    }

    public async Task<bool> CanRestartAsync()
    {
        if (++_nrRestarts >= _maxRestarts)
            return false;

        await Task.Delay(_delayMillis);
        return true;
    }
}
