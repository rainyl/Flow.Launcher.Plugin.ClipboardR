namespace ClipboardR.Core;

public static class Utils
{
    private static Random _random = new();

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[_random.Next(s.Length)]).ToArray()
        );
    }

    public static string GetGuid()
    {
        return Guid.NewGuid().ToString("D");
    }

    public static string? SaveImageCache(ClipboardData clipboardData, DirectoryInfo clipCacheDir)
    {
        if (clipboardData.Data is not Image img)
            return null;
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
        Do<object>(
            () =>
            {
                action();
                return null;
            },
            retryInterval,
            maxAttemptCount
        );
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

public static class CmBoxIndexMapper
{
    private static readonly Dictionary<int, int> KeepTimeDict =
        new(
            new List<KeyValuePair<int, int>>()
            {
                new(0, int.MaxValue),
                new(1, 1),
                new(2, 12),
                new(3, 24),
                new(4, 72),
                new(5, 168),
                new(6, 720),
                new(7, 4320),
                new(8, 8640),
            }
        );

    private static readonly Dictionary<int, CbOrders> OrderByDict =
        new(
            new List<KeyValuePair<int, CbOrders>>
            {
                new(0, CbOrders.Score),
                new(1, CbOrders.CreateTime),
                new(2, CbOrders.SourceApplication),
                new(3, CbOrders.Type),
            }
        );

    public static int ToKeepTime(int idx)
    {
        var k = KeepTimeDict.ContainsKey(idx) ? idx : 0;
        return KeepTimeDict[k];
    }

    public static CbOrders ToOrderBy(int idx)
    {
        var k = OrderByDict.ContainsKey(idx) ? idx : 0;
        return OrderByDict[k];
    }
}
