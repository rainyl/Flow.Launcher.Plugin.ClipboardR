using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace ClipboardR.Core;

public static class ClipImageExtension
{
    public static BitmapImage ToBitmapImage(this Image img)
    {
        var stream = new MemoryStream();
        img.Save(stream, ImageFormat.Png);
        var im = new BitmapImage();
        im.BeginInit();
        im.CacheOption = BitmapCacheOption.OnLoad;
        im.StreamSource = stream;
        im.EndInit();
        stream.Close();
        return im;
    }
}