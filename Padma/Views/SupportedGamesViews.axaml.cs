using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Padma.ViewModels;

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

        // Double check DataContext here too
        if (DataContext is not SupportedGamesViewModel && _viewModel != null) DataContext = _viewModel;
    }
}