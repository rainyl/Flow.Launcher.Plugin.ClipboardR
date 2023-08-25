namespace ClipboardR.Core;

public static class Utils
{
    private static Random _random = new();

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    
    public static string? SaveImageCache(ClipboardData clipboardData, DirectoryInfo clipCacheDir)
    {
        if (clipboardData.Data is not Image img) return null;
        var name = RandomString(10);
        var path = Path.Join(clipCacheDir.FullName, $"{name}.png");

        img.Save(path);
        return path;
    }
}

public static class Retry
{
    public static void Do(Action action, int retryInterval, int maxAttemptCount = 3)
    {
        Do<object>(() =>
        {
            action();
            return null;
        }, retryInterval, maxAttemptCount);
    }

    public static T Do<T>(Func<T> action, int retryInterval, int maxAttemptCount = 3)
    {
        var exceptions = new List<Exception>();

        for (int attempted = 0; attempted < maxAttemptCount; attempted++)
        {
            try
            {
                if (attempted > 0)
                    Thread.Sleep(retryInterval);
                return action();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException(exceptions);
    }
}