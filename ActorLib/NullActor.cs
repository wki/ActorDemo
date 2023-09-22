namespace ActorLib;

/// <summary>
/// A valid actor instead of null
/// </summary>
/// <remarks>
/// it can receive messages but will not do anything.
/// </remarks>
public class NullActor : Actor
{
    public static NullActor Instance = new NullActor();

    private NullActor(): base(null, "NullActor") {}

    protected override Task OnReceiveAsync(object message) =>
        Task.CompletedTask;
}