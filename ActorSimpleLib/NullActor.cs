namespace ActorSimpleLib;

/// <summary>
/// A valid actor instead of null
/// </summary>
/// <remarks>
/// it can receive messages but will not do anything.
/// </remarks>
public class NullActor : Actor
{
    public static readonly NullActor Instance = new NullActor(null, "NullActor");

    private NullActor(IActorRef parent, string name) : base(parent, name) {}

    protected override Task OnReceiveAsync(object message) =>
        Task.CompletedTask;
}