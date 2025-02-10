using Avalonia.Controls;
using Avalonia;
using System;
using Padma.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Padma.Views;

public partial class SupportedGamesViews : UserControl
{
    private readonly SupportedGamesViewModel _viewModel;
    public SupportedGamesViews()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<SupportedGamesViewModel>();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        Console.WriteLine($"OnAttachedToVisualTree - Before check, DataContext type: {DataContext?.GetType().Name}");
        
        // Double check DataContext here too
        if (DataContext is not SupportedGamesViewModel && _viewModel != null)
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

