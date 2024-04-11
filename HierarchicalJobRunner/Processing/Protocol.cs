namespace HierarchicalJobRunner.Processing;

// tell an executor to start execution
public record Start;

// initiator of Start() will receive this message
public record Finished(RunStatus RunStatus);

// tell an executor to cancel execution for an element given by Id
public record Cancel(Guid Id);

// tell an executor to retry execution of a canceled or failed element given by Id
public record Retry(Guid Id);

// tell an executor to skip a canceled or failed element given by Id
public record Skip(Guid Id);

// executor tells its parent that it has started
public record ChildStarted(Guid Id);

// executor tells its parent that it has failed
public record ChildFailed(Guid Id);

// executor tells its parent that it has skipped
public record ChildSkipped(Guid Id);

// executor tells its parent that it has completed
public record ChildCompleted(Guid Id); // FIXME: result?

public record ChildCanceled(Guid Id); // FIXME: reason?
