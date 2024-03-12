using HierarchicalJobRunner.Job;

namespace HierarchicalJobRunner.Processing;

public interface IRunner
{
    Task RunAsync(CancellationToken cancellationToken);
}

// public interface IRunner<T> : IRunner
// {
// }

// public class RunResult<TResult>(RunStatus runStatus, TResult? result)
// {
//     public RunStatus RunStatus { get; set; } = runStatus;
//     public TResult? Result { get; set; } = result;
// }

// public enum RunStatus
// {
//     /// <summary>
//     /// still running or not known
//     /// </summary>
//     Unknown,
//     
//     /// <summary>
//     /// canceled by user
//     /// </summary>
//     Canceled,
//     
//     /// <summary>
//     /// running failed
//     /// </summary>
//     Failed,
//     
//     /// <summary>
//     /// Timeout occured
//     /// </summary>
//     Timeout,
//     
//     /// <summary>
//     /// ran to completion
//     /// </summary>
//     Complete
// }
