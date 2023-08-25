using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using WindowsInput;
using ClipboardR.Core;
using ClipboardR.Panels;

namespace ClipboardR;

public class ClipboardR : IPlugin, IDisposable, ISettingProvider, ISavable
{
    // clipboard listener instance
    private CbMonitor _clipboard = new() { ObserveLastEntry = false };
    private string _className => GetType().Name;

    // working directory
    private DirectoryInfo ClipDir { get; set; } = null!;

    private DirectoryInfo ClipCacheDir { get; set; } = null!;

    // default icon path
    private string _defaultIconPath = null!;
    private string _defaultPinIconPath = null!;

    private string _settingsPath = null!;

    // max data count, will be rewritten by settings
    private int _maxDataCount = 10000;
    private const string PinUnicode = "📌";
    private Settings _settings = null!;
    private int CurrentScore { get; set; } = 1;

    private DbHelper _dbHelper = null!;
    private string _dbPath = null!;

    private PluginInitContext? _context;
    private LinkedList<ClipboardData> _dataList = new();

    public void Init(PluginInitContext ctx)
    {
        this._context = ctx;
        _context.API.LogDebug(_className, "Adding clipboard listener");
        this._clipboard.ClipboardChanged += _OnClipboardChange;

        ClipDir = new DirectoryInfo(ctx.CurrentPluginMetadata.PluginDirectory);
        var imageCacheDirectoryPath = Path.Combine(ClipDir.FullName, "CachedImages");
        ClipCacheDir = !Directory.Exists(imageCacheDirectoryPath)
            ? Directory.CreateDirectory(imageCacheDirectoryPath)
            : new DirectoryInfo(imageCacheDirectoryPath);

        _defaultIconPath = Path.Join(ClipDir.FullName, "Images/clipboard.png");
        _defaultPinIconPath = Path.Join(ClipDir.FullName, "Images/clipboard_pined.png");

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
        _context.API.LogDebug(_className, "Created settings successfully");

        _maxDataCount = _settings.MaxDataCount;
        // restore records
        _context.API.LogDebug(_className, $"{_settings.ToString()}");
        _dbPath = Path.Join(ClipDir.FullName, _settings.DbPath);
        _context.API.LogDebug(_className, $"Using database at: {_dbPath}");
        _dbHelper = new DbHelper($"Data Source={_dbPath}");
        RestoreRecordsFromDb();
    }

    private void RestoreRecordsFromDb()
    {
        if (!File.Exists(_dbPath))
        {
            _dbHelper.CreateDb();
            return;
        }

        var records = _dbHelper.GetAllRecord();
        if (records.Count > 0)
            _dataList = records;
        _context!.API.LogWarn(_className, "Restore records successfully");
    }

    public List<Result> Query(Query query)
    {
        var displayData =
            query.Search.Trim().Length == 0
                ? _dataList.ToArray()
                : _dataList
                    .Where(
                        i =>
                            !string.IsNullOrEmpty(i.Text)
                            && i.Text.ToLower().Contains(query.Search.Trim().ToLower())
                    )
                    .ToArray();

        var results = new List<Result>();
        results.AddRange(displayData.Select(ClipDataToResult));
        _context.API.LogWarn(_className, "Added to result");
        results.Add(
            new Result()
            {
                Title = "Clear All Records",
                SubTitle = "Click to clear all records",
                IcoPath = _defaultIconPath,
                Score = 1,
                Action = _ =>
                {
                    _dataList.Clear();
                    // TODO: Add option in settings to select clear records from database
                    CurrentScore = 1;
                    return true;
                },
            }
        );
        return results;
    }

    private Result ClipDataToResult(ClipboardData o)
    {
        var dispSubTitle = $"{o.CreateTime:yyyy-MM-dd-hh-mm-ss}: {o.SenderApp}";
        dispSubTitle = o.Pined ? $"{PinUnicode}{dispSubTitle}" : dispSubTitle;
        return new Result
        {
            Title = o.DisplayTitle,
            SubTitle = dispSubTitle,
            Icon = () => o.Icon,
            CopyText = o.Text,
            Score = o.Score,
            TitleToolTip = o.Text,
            SubTitleToolTip = dispSubTitle,
            PreviewPanel = new Lazy<UserControl>(
                () =>
                    new PreviewPanel(
                        o,
                        _context!,
                        ClipCacheDir,
                        delAction: RemoveFromDatalist,
                        copyAction: CopyToClipboard,
                        pinAction: PinOneRecord
                    )
            ),
            AsyncAction = async _ =>
            {
                CopyToClipboard(o);
                _context!.API.HideMainWindow();
                while (_context!.API.IsMainWindowVisible())
                    await Task.Delay(100);
                new InputSimulator().Keyboard.ModifiedKeyStroke(
                    VirtualKeyCode.CONTROL,
                    VirtualKeyCode.VK_V
                );
                return true;
            },
        };
    }

    private void _OnClipboardChange(object? sender, CbMonitor.ClipboardChangedEventArgs e)
    {
        _context!.API.LogDebug(_className, "Clipboard changed");
        if (e.Content is null)
            return;

        var now = DateTime.Now;
        var clipboardData = new ClipboardData
        {
            Text = "",
            Type = e.ContentType,
            Data = e.Content,
            SenderApp = e.SourceApplication.Name,
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            PreviewImagePath = _defaultIconPath,
            Score = CurrentScore + 1,
            InitScore = CurrentScore + 1,
            Time = now,
            Pined = false,
            CreateTime = now,
        };

        switch (e.ContentType)
        {
            case CbMonitor.ContentTypes.Text:
                clipboardData.Text = _clipboard.ClipboardText;
                _context.API.LogDebug(_className, "Processed text change");
                break;
            case CbMonitor.ContentTypes.Image:
                clipboardData.Text = $"Image:{clipboardData.Time:yy-MM-dd-HH:mm:ss}";
                if (_settings.CacheImages)
                    Utils.SaveImageCache(clipboardData, ClipCacheDir);
                clipboardData.Icon = _clipboard.ClipboardImage.ToBitmapImage();
                _context.API.LogDebug(_className, "Processed image change");
                break;
            case CbMonitor.ContentTypes.Files:
                var t = _clipboard.ClipboardFiles.ToArray();
                clipboardData.Data = t;
                clipboardData.Text = string.Join("\n", t.Take(2)) + "\n...";
                _context.API.LogDebug(_className, "Processed file change");
                break;
            case CbMonitor.ContentTypes.Other:
                // TODO: nothing to do now
                _context.API.LogDebug(_className, "Other change listened, skip");
                return;
            default:
                break;
        }

        clipboardData.DisplayTitle = Regex.Replace(clipboardData.Text.Trim(), @"(\r|\n|\t|\v)", "");

        // make sure no repeat
        if (_dataList.Any(node => node.Equals(clipboardData)))
            return;
        _context.API.LogDebug(_className, "Adding to dataList");
        _dataList.AddFirst(clipboardData);
        _context.API.LogDebug(_className, "Adding to database");
        var isAdd = (_settings.KeepText && clipboardData.Type == CbMonitor.ContentTypes.Text) ||
                    (_settings.KeepImages && clipboardData.Type == CbMonitor.ContentTypes.Image) ||
                    (_settings.KeepFiles && clipboardData.Type == CbMonitor.ContentTypes.Files);
        if (isAdd)
            _dbHelper.AddOneRecord(clipboardData);
        if (_dataList.Count > _maxDataCount)
            _dataList.RemoveLast();
        CurrentScore++;
        _context.API.LogDebug(_className, "Processing clipboard change finished");
    }

    public Control CreateSettingPanel() => new SettingsPanel(_settings, _context!);

    public void Dispose()
    {
        try
        {
            // Delete expired records
            _dbHelper?.DeleteRecordByKeepTime((int)CbMonitor.ContentTypes.Text, _settings.KeepTextHours);
            _dbHelper?.Close();
        }
        catch (Exception e)
        {
            // ignore
        }
    }

    public void CopyToClipboard(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        System.Windows.Forms.Clipboard.SetDataObject(clipboardData.Data);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void RemoveFromDatalist(ClipboardData clipboardData)
    {
        _dataList.Remove(clipboardData);
        _dbHelper.DeleteOneRecord(clipboardData);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void PinOneRecord(ClipboardData c)
    {
        _dataList.Remove(c);
        if (c.Type is CbMonitor.ContentTypes.Text or CbMonitor.ContentTypes.Files)
            c.Icon = c.Pined
                ? new BitmapImage(new Uri(_defaultPinIconPath, UriKind.RelativeOrAbsolute))
                : new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute));
        _dataList.AddLast(c);
        _dbHelper.PinOneRecord(c);
        _context!.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword, true);
    }

    public void Save()
    {
        _settings?.Save();
    }
}