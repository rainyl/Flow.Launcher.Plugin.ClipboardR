using System.Windows.Media;
using WK.Libraries.SharpClipboardNS;

namespace ClipboardR.Core;

public struct ClipboardData
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
}