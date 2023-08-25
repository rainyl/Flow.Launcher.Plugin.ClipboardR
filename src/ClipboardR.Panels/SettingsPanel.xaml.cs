using System;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using ClipboardR.Core;

namespace ClipboardR.Panels;

public partial class SettingsPanel
{
    public Settings settings { get; set; }
    private PluginInitContext? _context { get; set; }

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
        _context = null;
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
        _context?.API.RestartApp();
    }

    private void SpinBoxMaxRec_OnValueChanged(int v)
    {
        MaxDataCount = v;
    }

    private void CkBoxKeepText_OnChecked(object sender, RoutedEventArgs e)
    {
        this.settings.KeepText = true;
    }

    private void CkBoxKeepText_OnUnchecked(object sender, RoutedEventArgs e)
    {
        this.settings.KeepText = false;
    }

    private void CkBoxKeepImages_OnChecked(object sender, RoutedEventArgs e)
    {
        settings.KeepImages = true;
    }

    private void CkBoxKeepImages_OnUnchecked(object sender, RoutedEventArgs e)
    {
        settings.KeepImages = false;
    }

    private void CkBoxKeepFiles_OnChecked(object sender, RoutedEventArgs e)
    {
        settings.KeepFiles = true;
    }

    private void CkBoxKeepFiles_OnUnchecked(object sender, RoutedEventArgs e)
    {
        settings.KeepFiles = false;
    }

    private void CmBoxKeepText_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        settings.KeepTextHours = TryGetCBoxTag(sender);
    }

    private void CmBoxKeepImages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        settings.KeepImagesHours = TryGetCBoxTag(sender);
    }

    private void CmBoxKeepFiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        settings.KeepFilesHours = TryGetCBoxTag(sender);
    }

    private uint TryGetCBoxTag(object sender)
    {
        var item = (sender as ComboBox)?.SelectedItem as ComboBoxItem;
        if (item?.Tag == null) return uint.MaxValue;
        var success = uint.TryParse(item.Tag as string, out var v);
        if (!success) return uint.MaxValue;
        return v == 0 ? uint.MaxValue : v;
    }
}