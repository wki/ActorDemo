namespace ActorLib;

/// <summary>
/// A valid actor instead of null
/// </summary>
/// <remarks>
/// it can receive messages but will not do anything.
/// </remarks>
public class NullActor : Actor
{
    public static readonly NullActor Instance = new();

    private NullActor()
    {
        Name = "null";
    }
}
