namespace ActorSimpleLib;

public record Envelope(Actor Sender, Actor Receiver, object Message);