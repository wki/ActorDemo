namespace ActorLib;

public class AskTimeoutException : Exception
{
    public AskTimeoutException(string message): base(message) { }
}