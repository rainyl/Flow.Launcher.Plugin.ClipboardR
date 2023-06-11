using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin;
using ClipboardR.Core;

namespace ClipboardR.Panels;

public partial class SettingsPanel : UserControl
{
    public Settings settings { get; set; }
    private PluginInitContext _context { get; set; }

    public static readonly DependencyProperty MaxDataCountProperty = DependencyProperty.Register(
        nameof(MaxDataCount), typeof(int), typeof(SettingsPanel), new PropertyMetadata(default(int)));

    public int MaxDataCount
    {
        get => settings.MaxDataCount;
        set
        {
            SetValue(MaxDataCountProperty, value);
            settings.MaxDataCount = value;
            SpinBoxMaxRec.Value = value;
        }
    }
    
    public SettingsPanel(Settings settings, PluginInitContext ctx)
    {
        this.settings = settings;
        _context = ctx;
        InitializeComponent();
        MaxDataCount = settings.MaxDataCount;
    }

    /// <summary>
    /// Note: For Test UI Only !!!
    /// Any interaction with Flow.Launcher will cause exit
    /// </summary>
    public SettingsPanel()
    {
        this.settings = new Settings(){ConfigFile = "test.json"};
        _context = new PluginInitContext();
        InitializeComponent();
        MaxDataCount = settings.MaxDataCount;
        // SpinBoxMaxRec.ValueChanged += i => MessageBox.Show($"{i}");
    }

    private void CkBoxCacheImages_OnChecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox c)
            settings.CacheImages = c.IsChecked ?? false;
    }

    private void BtnRestartFlow_OnClick(object sender, RoutedEventArgs e)
    {
        _context.API.RestartApp();
    }

    private void SpinBoxMaxRec_OnValueChanged(int v)
    {
        MaxDataCount = v;
    }
}