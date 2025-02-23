using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Padma.ViewModels;

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

        // Double check DataContext here too
        if (DataContext is not HistoryViewModel && _viewModel != null) DataContext = _viewModel;
    }
}