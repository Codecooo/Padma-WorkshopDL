using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Padma.ViewModels;
using System;
using Avalonia;

namespace Padma.Views;

public partial class SettingsViews : UserControl
{
    private readonly SettingsViewModel _viewModel;
    public SettingsViews()
    {
        InitializeComponent();
        // Get instance through Dependency Injection in App.axaml.cs to ensure its running on the same instance as everyone else
        _viewModel = App.ServiceProvider.GetRequiredService<SettingsViewModel>();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        Console.WriteLine($"OnAttachedToVisualTree - Before check, DataContext type: {DataContext?.GetType().Name}");
        
        // Double check DataContext here too
        if (DataContext is not SettingsViewModel && _viewModel != null)
        {
            Console.WriteLine("Resetting DataContext in OnAttachedToVisualTree");
            DataContext = _viewModel;
        }
    
        // if (DataContext is SupportedGamesViewModel vm)
        // {
        //     Console.WriteLine($"OnAttachedToVisualTree - WorkshopTitle: {vm.WorkshopTitle}");
        //     Console.WriteLine($"OnAttachedToVisualTree - IsEnabled: {vm.IsEnabled}");
        // }
        else
        {
            Console.WriteLine("OnAttachedToVisualTree - DataContext is not HomeViewModel");
        }
    }
}