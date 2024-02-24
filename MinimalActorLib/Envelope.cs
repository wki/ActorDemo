namespace MinimalActorLib;

/// <summary>
/// Group sender and message together to a single unit for transporting
/// </summary>
/// <param name="Sender"></param>
/// <param name="Message"></param>
internal record Envelope(Actor Sender, object Message);