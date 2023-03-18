namespace ActorLib;

public record Envelope(IActorRef Sender, IActorRef Receiver, object Message);