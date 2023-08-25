using System.Text.Json;

namespace ClipboardR.Core;

public class Settings
{
    public string ConfigFile = null!;

    public bool CacheImages { get; set; } = false;
    public bool KeepText { get; set; } = false;
    public uint KeepTextHours { get; set; } = uint.MaxValue;
    public bool KeepImages { get; set; } = false;
    public uint KeepImagesHours { get; set; } = uint.MaxValue;
    public bool KeepFiles { get; set; } = false;
    public uint KeepFilesHours { get; set; } = uint.MaxValue;

    public int MaxDataCount { get; set; } = 10000;

    public string DbPath { get; set; } = "ClipboardR.db";

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(this, options));
    }

    public override string ToString()
    {
        var type = GetType();
        var props = type.GetProperties();
        var s = props.Aggregate("Settings(\n", (current, prop) => current + $"\t{prop.Name}: {prop.GetValue(this)}\n");
        s += ")";
        return s;
    }
}