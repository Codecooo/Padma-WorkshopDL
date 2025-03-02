using System;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Padma.ViewModels;

namespace Padma.Views;

public partial class HomeViews : UserControl
{
    private readonly HomeViewModel _homeViewModel;

    public HomeViews()
    {
        InitializeComponent();

        // Get the ViewModel but don't set DataContext yet
        _homeViewModel = App.ServiceProvider.GetRequiredService<HomeViewModel>();

        AutoScrollLogs();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // If DataContext is null or not our ViewModel, set it
        if (DataContext is not HomeViewModel) DataContext = _homeViewModel;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Double check DataContext here too
        if (DataContext is not HomeViewModel && _homeViewModel != null) DataContext = _homeViewModel;
    }

    private void AutoScrollLogs()
    {
        var logoutput = this.FindControl<TextBox>("LogOutput");
        logoutput.GetObservable<string>(TextBlock.TextProperty);
        logoutput.GetObservable<string>(TextBox.TextProperty)
            .Subscribe(_ =>
            {
                // Set the caret index to the end of the text
                if (logoutput.Text != null) logoutput.CaretIndex = logoutput.Text.Length;
            });
    }
}