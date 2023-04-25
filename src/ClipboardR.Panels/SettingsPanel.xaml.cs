using System;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin;
using ClipboardR.Core;

namespace ClipboardR.Panels;

public partial class SettingsPanel : UserControl
{
    public Settings settings { get; set; }
    // public ICommand OnCkboxCacheImagesChanged { get; set; }
    private PluginInitContext _context { get; set; }
    public SettingsPanel(Settings settings, PluginInitContext ctx)
    {
        this.settings = settings;
        _context = ctx;
        InitializeComponent();
        // TODO: Won't work
        // OnCkboxCacheImagesChanged = new RelayCommand((p) =>
        // {
        //     var isChecked = (bool)p;
        //     if (settings.CacheImages == isChecked) return;
        //     settings.CacheImages = isChecked;
        //     _context.API.ShowMsg($"{isChecked}");
        // });
    }

    // public class RelayCommand : ICommand
    // {
    //     public event EventHandler? CanExecuteChanged;
    //     private Action<object> _action;
    //
    //     public RelayCommand(Action<object> action)
    //     {
    //         _action = action;
    //     }
    //     public bool CanExecute(object? o)
    //     {
    //         return true;
    //     }
    //
    //     public void Execute(object? parameter)
    //     {
    //         if (parameter is null) return;
    //         var p = (bool)parameter;
    //         _action?.Invoke(p);
    //     }
    // }
}