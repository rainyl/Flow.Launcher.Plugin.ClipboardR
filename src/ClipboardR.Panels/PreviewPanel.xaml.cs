using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Flow.Launcher.Plugin;
using ClipboardR.Core;
using WK.Libraries.SharpClipboardNS;
using Image = System.Drawing.Image;

namespace ClipboardR.Panels;

public partial class PreviewPanel : UserControl
{
    private ClipboardData _clipboardData;
    private PluginInitContext _context;
    private Action<ClipboardData> DeleteOneRecord { get; set; }
    private Action<ClipboardData> CopyRecord { get; set; }
    public PreviewPanel(ClipboardData clipboardData, PluginInitContext context, Action<ClipboardData> delAction, Action<ClipboardData> copyAction)
    {
        _clipboardData = clipboardData;
        _context = context;
        DeleteOneRecord = delAction;
        CopyRecord = copyAction;
        InitializeComponent();
        
        SetContent();
    }

    public void SetContent()
    {
        TxtBoxPre.Visibility = Visibility.Visible;
        PreImage.Visibility = Visibility.Hidden;
        switch (_clipboardData.Type)
        {
            case SharpClipboard.ContentTypes.Text:
                SetText();
                break;
            case SharpClipboard.ContentTypes.Files:
                var ss = _clipboardData.Data as string[] ?? Array.Empty<string>();
                var s = string.Join('\n', ss);
                SetText(s);
                break;
            case SharpClipboard.ContentTypes.Image:
                TxtBoxPre.Visibility = Visibility.Hidden;
                PreImage.Visibility = Visibility.Visible;
                SetImage();
                break;
            case SharpClipboard.ContentTypes.Other:
            default:
                break;
        }
    }
    
    public void SetText(string s = "")
    {
        TxtBoxPre.Clear();
        TxtBoxPre.Text = string.IsNullOrWhiteSpace(s) ? _clipboardData.Text : s;
    }

    public void SetImage()
    {
        if (_clipboardData.Data is not Image img) return;
        var im = img.ToBitmapImage();
        PreImage.Source = im;
    }

    private void BtnCopy_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        CopyRecord?.Invoke(_clipboardData);
    }

    private void BtnDelete_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DeleteOneRecord?.Invoke(_clipboardData);
    }

    private void TxtBoxPre_GotFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        TxtBoxPre.SelectAll();
    }

    private void TxtBoxPre_TextChanged(object sender, TextChangedEventArgs e)
    {

    }
}