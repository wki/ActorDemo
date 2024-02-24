namespace MinimalActorLib;

/// <summary>
/// Timeout message singleton
/// </summary>
public class TimeOut
{
    public static readonly TimeOut Instance = new();
    private TimeOut(){}
}