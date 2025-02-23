using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Padma.ViewModels;

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

        // Double check DataContext here too
        if (DataContext is not SettingsViewModel && _viewModel != null) DataContext = _viewModel;
    }
}