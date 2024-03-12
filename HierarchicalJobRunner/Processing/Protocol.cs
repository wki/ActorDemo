namespace HierarchicalJobRunner.Processing;

// tell an executor to start execution
public record Start();

public record Cancel();

// executor tells its parent that it has started
public record ChildStarted(Guid Id);

// executor tells its parent that it has failed
public record ChildFailed(Guid Id);

// executor tells its parent that it has completed
public record ChildCompleted(Guid Id);

public record ChildCanceled(Guid Id);

public record ChildTimedOut(Guid Id);