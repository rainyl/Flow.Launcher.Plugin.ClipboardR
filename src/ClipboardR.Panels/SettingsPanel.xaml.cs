using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin;
using ClipboardR.Core;
using Xceed.Wpf.Toolkit;

namespace ClipboardR.Panels;

public partial class SettingsPanel : UserControl
{
    public Settings settings { get; set; }
    private PluginInitContext _context { get; set; }
    public SettingsPanel(Settings settings, PluginInitContext ctx)
    {
        this.settings = settings;
        _context = ctx;
        InitializeComponent();
    }

    private void CkBoxCacheImages_OnChecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox c)
            settings.CacheImages = c.IsChecked ?? false;
    }

    private void UpDownMaxRecCount_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (sender is UIntegerUpDown { Value: { } } u)
            settings.MaxDataCount = u.Value.Value;
    }

    private void BtnRestartFlow_OnClick(object sender, RoutedEventArgs e)
    {
        _context.API.RestartApp();
    }
}