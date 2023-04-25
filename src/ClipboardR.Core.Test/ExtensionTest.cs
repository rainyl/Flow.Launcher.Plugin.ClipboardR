using System.Windows.Media.Imaging;
using Xunit.Abstractions;

namespace ClipboardR.Core.Test;
using System.Drawing;
using Image = System.Drawing.Image;
using ClipboardR.Core;
public class ExtensionTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ExtensionTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Test1()
    {
        var filename = @"F:\cSharp\Flow.Launcher.Plugin.Clipboard\src\ClipboardR\Images\clipboard.png";
        var img = new Bitmap(filename);
        _testOutputHelper.WriteLine(img.RawFormat.ToString());
        var im = img.ToBitmapImage();
        BitmapImage bm = new BitmapImage(new Uri(filename, UriKind.RelativeOrAbsolute));

    }
}