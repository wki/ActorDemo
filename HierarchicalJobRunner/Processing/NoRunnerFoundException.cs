namespace HierarchicalJobRunner.Processing;

public class NoRunnerFoundException : Exception
{
    public NoRunnerFoundException(string message): base(message)
    {
    }
}