namespace ActorLib.Ask;

public class AskTimeoutException : Exception
{
    public AskTimeoutException(string s): base(s)
    {
    }
}