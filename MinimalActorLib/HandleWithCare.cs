namespace MinimalActorLib;

internal static class HandleWithCare
{
    public static async Task IgnoreExceptions(this Task taskWhichCouldThrow)
    {
        try
        {
            await taskWhichCouldThrow;
        }
        catch
        {
            // simply ignore errors
        }
    }

    public static async Task<T> IgnoreExceptions<T>(this Task<T> taskWhichCouldThrow)
    {
        try
        {
            return await taskWhichCouldThrow;
        }
        catch
        {
            // simply ignore errors
        }

        return default!;
    }
}