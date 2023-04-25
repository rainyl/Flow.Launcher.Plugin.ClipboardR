using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;
using Flow.Launcher.Plugin;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WindowsInput;
using WK.Libraries.SharpClipboardNS;
using Image = System.Drawing.Image;
using ClipboardR.Core;
using ClipboardR.Panels;

namespace ClipboardR;

public class ClipboardR : IPlugin, IDisposable, ISettingProvider, ISavable
{
    private SharpClipboard _clipboard = new();
    private DirectoryInfo ClipDir { get; set; } = null!;
    private DirectoryInfo ClipCacheDir { get; set; } = null!;
    private string _defaultIconPath = null!;
    private const int MaxDataCount = 1000;
    private const int MaxTitleLength = 30;
    private static Random _random = new();
    private Settings _settings = null!;
    private string _settingsPath = null!;
    private int CurrentScore { get; set; } = 0;

    // private readonly InputSimulator inputSimulator = new InputSimulator();
    private PluginInitContext? _context;
    private LinkedList<ClipboardData> _dataList = new();

    public void Init(PluginInitContext ctx)
    {
        this._context = ctx;
        this._clipboard.ClipboardChanged += _OnClipboardChange;

        ClipDir = new DirectoryInfo(ctx.CurrentPluginMetadata.PluginDirectory);
        var imageCacheDirectoryPath = Path.Combine(ClipDir.FullName, "CachedImages");
        ClipCacheDir = !Directory.Exists(imageCacheDirectoryPath)
            ? Directory.CreateDirectory(imageCacheDirectoryPath)
            : new DirectoryInfo(imageCacheDirectoryPath);

        _defaultIconPath = Path.Join(ClipDir.FullName, "Images/clipboard.png");

        _settingsPath = Path.Join(ClipDir.FullName, "settings.json");
        if (File.Exists(_settingsPath))
        {
            using var fs = File.OpenRead(_settingsPath);
            _settings = JsonSerializer.Deserialize<Settings>(fs)!;
        }
        else
            _settings = new Settings();

        _settings.ConfigFile = _settingsPath;
        _settings.Save();
    }

    public List<Result> Query(Query query)
    {
        var displayData = query.Search.Trim().Length == 0
            ? _dataList
            : _dataList.Where(i => !string.IsNullOrEmpty(i.Text) && i.Text.ToLower().Contains(query.Search.Trim().ToLower()));

        var results = new List<Result>();
        results.AddRange(displayData.Select(o => new Result
        {
            Title = o.DisplayTitle,
            SubTitle = o.SenderApp,
            // IcoPath = o.IconPath,
            Icon = () => o.Icon,
            CopyText = o.Text,
            Score = o.Score,
            TitleToolTip = o.Text,
            SubTitleToolTip = o.SenderApp,
            // Preview = new Result.PreviewInfo
            // {
            //     Description = o.Text,
            //     PreviewImagePath = o.PreviewImagePath,
            //     IsMedia = true,
            // },
            PreviewPanel = new Lazy<UserControl>(() => new PreviewPanel(
                o,
                _context!,
                delAction: RemoveFromDatalist,
                copyAction: CopyToClipboard
            )),
            Action = c =>
            {
                // _context!.API.ShowMsg("Wow");
                CopyToClipboard(o);
                // Due to the focus will change when open FlowLauncher, this won't work for now
                new InputSimulator().Keyboard
                    .ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                return true;
            }
        }));
        return results;
    }

    private void _OnClipboardChange(object? sender, SharpClipboard.ClipboardChangedEventArgs e)
    {
        if (e.Content is null) return;
        ClipboardData clipboardData = new ClipboardData
        {
            Text = "",
            Type = e.ContentType,
            Data = e.Content,
            SenderApp = e.SourceApplication.Name,
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            PreviewImagePath = _defaultIconPath,
            Score = CurrentScore++,
        };
        switch (e.ContentType)
        {
            case SharpClipboard.ContentTypes.Text:
                clipboardData.Text = _clipboard.ClipboardText;
                break;
            case SharpClipboard.ContentTypes.Image:
                clipboardData.Text = "Image";
                // string? p = null;
                if (_settings.CacheImages) SaveImageCache(clipboardData);
                // if (p != null)
                // {
                //     clipboardData.PreviewImagePath = p;
                //     clipboardData.IconPath = p;
                //     var img = Image.FromFile(p);
                //     clipboardData.Icon = img.ToBitmapImage();
                // }
                clipboardData.Icon = _clipboard.ClipboardImage.ToBitmapImage();

                break;
            case SharpClipboard.ContentTypes.Files:
                var t = _clipboard.ClipboardFiles.ToArray();
                clipboardData.Data = t;
                clipboardData.Text = string.Join("\n", t.Take(2)) + "\n...";
                break;
            case SharpClipboard.ContentTypes.Other:
                // TODO: nothing to do now
                return;
            default:
                break;
        }

        clipboardData.DisplayTitle =
            clipboardData.Text.Length > MaxTitleLength
                ? clipboardData.Text[..MaxTitleLength].Trim() + "..."
                : clipboardData.Text;

        _dataList.AddFirst(clipboardData);
        if (_dataList.Count > MaxDataCount)
        {
            _dataList.RemoveLast();
        }
    }

    private string? SaveImageCache(ClipboardData clipboardData)
    {
        if (clipboardData.Data is not Image img) return null;
        var name = RandomString(10);
        var path = Path.Join(ClipCacheDir.FullName, $"{name}.png");

        img.Save(path);
        return path;
    }

    public Control CreateSettingPanel() => new SettingsPanel(_settings, _context!);

    private string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public void Dispose()
    {
    }

    public void CopyToClipboard(ClipboardData clipboardData)
    {
        System.Windows.Forms.Clipboard.SetDataObject(clipboardData.Data);
        _dataList.Remove(clipboardData);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void RemoveFromDatalist(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void Save()
    {
        _settings?.Save();
    }
}