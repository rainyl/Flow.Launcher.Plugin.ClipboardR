using System.Windows.Media.Imaging;

namespace ClipboardR.Core;

public struct ClipboardData : IEquatable<ClipboardData>
{
    public object Data;
    public string Text;
    public string DisplayTitle;
    public string SenderApp;
    public string IconPath;
    public BitmapImage Icon;
    public string PreviewImagePath; // actually not used for now
    public CbMonitor.ContentTypes Type;
    public int Score;
    public int InitScore;
    public DateTime Time;
    public bool Pined;
    public DateTime CreateTime;

    public bool Equals(ClipboardData b)
    {
        return GetHashCode() == b.GetHashCode();
    }

    public override int GetHashCode()
    {
        var hashcode = (Text?.GetHashCode() ?? 0) ^ (DisplayTitle?.GetHashCode() ?? 0) ^
                       (Data?.GetHashCode() ?? 0) ^ (SenderApp?.GetHashCode() ?? 0) ^ Type.GetHashCode();
        return hashcode;
    }

    public string GetMd5()
    {
        return Text.GetMd5() + DisplayTitle.GetMd5();
    }
}