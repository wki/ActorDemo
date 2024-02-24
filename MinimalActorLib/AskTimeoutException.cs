namespace MinimalActorLib;

/// <summary>
/// Indicates that a timeout has occured during asking an actor for an answer
/// </summary>
internal class AskTimeoutException : Exception
{
    public AskTimeoutException(string message): base(message) { }
}