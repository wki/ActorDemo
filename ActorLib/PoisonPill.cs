namespace ActorLib;

/// <summary>
/// message terminating an actor when it receives it.
/// </summary>
public class PoisonPill
{
    private static PoisonPill? _instance;
    public static PoisonPill Instance => _instance ??= new PoisonPill();  

    private PoisonPill() { }
}