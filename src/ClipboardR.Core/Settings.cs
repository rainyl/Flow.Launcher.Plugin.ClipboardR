using System.IO;
using System.Text.Json;

namespace ClipboardR.Core;
public class Settings
{
    public string ConfigFile = null!;

    public bool CacheImages { get; set; } = false;
    public uint MaxDataCount { get; set; } = 10000;

    public void Save()
    {
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(this));
    }
}
