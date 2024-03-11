namespace HierarchicalJobRunner.Processing;

// tell an executor to start execution
public record Start();

// executor tells its parent that it has started
public record Started(Guid id);

// executor tells its parent that it has failed
public record Failed(Guid id);

// executor tells its parent that it has completed
public record Completed(Guid id);
