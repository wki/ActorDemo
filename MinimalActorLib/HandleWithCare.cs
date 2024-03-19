namespace MinimalActorLib;

internal static class HandleWithCare
{
    public static async Task IgnoreExceptions(this Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // simply ignore errors
        }
    }

    public static async Task<T> IgnoreExceptions<T>(this Task<T> task)
    {
        try
        {
            return await task;
        }
        catch
        {
            // simply ignore errors
        }

        return default(T)!;
    }
}