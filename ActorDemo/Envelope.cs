namespace ActorDemo;

public record Envelope(IActorRef Sender, IActorRef Receiver, object Message);