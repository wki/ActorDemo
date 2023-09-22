namespace ActorLib;

public record Envelope(Actor Sender, Actor Receiver, object Message);