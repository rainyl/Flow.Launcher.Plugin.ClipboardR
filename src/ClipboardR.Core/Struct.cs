using System.Windows.Media;
using WK.Libraries.SharpClipboardNS;

namespace ClipboardR.Core;

public struct ClipboardData : IEquatable<ClipboardData>
{
    public object Data;
    public string Text;
    public string DisplayTitle;
    public string SenderApp;
    public string IconPath;
    public ImageSource Icon;
    public string PreviewImagePath;
    public SharpClipboard.ContentTypes Type;
    public int Score;
    public int InitScore;
    public DateTime Time;
    public bool Pined;

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
}