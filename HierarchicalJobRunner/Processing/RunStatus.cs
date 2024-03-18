namespace HierarchicalJobRunner.Processing;

public enum RunStatus
{
    Idle,
    Running,
    Completed,
    Skipped,
    Canceled,
    Failed,
}