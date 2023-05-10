using System.Drawing;

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