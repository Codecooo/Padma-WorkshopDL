using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Padma.ViewModels;
using Avalonia;
using System;

namespace Padma.Views;

public partial class HistoryViews : UserControl
{
    private readonly HistoryViewModel _viewModel;
    public HistoryViews()
    {
        InitializeComponent();
        _viewModel = App.ServiceProvider.GetRequiredService<HistoryViewModel>();
    }
    
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        Console.WriteLine($"OnAttachedToVisualTree - Before check, DataContext type: {DataContext?.GetType().Name}");
        
        // Double check DataContext here too
        if (DataContext is not HistoryViewModel && _viewModel != null)
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