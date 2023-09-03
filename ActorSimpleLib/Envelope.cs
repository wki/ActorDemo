namespace ActorSimpleLib;

public record Envelope(IActorRef Sender, IActorRef Receiver, object Message);