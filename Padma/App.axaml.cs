using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Padma.Models;
using Padma.ViewModels;
using Padma.Views;

namespace Padma;

public class App : Application
{
    public static IServiceProvider? ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        // Register services with DI container
        services.AddSingleton<SupportedGames>(); // Singleton for database access
        services.AddSingleton<SaveHistory>(); // Singleton for database access
        services.AddSingleton<SupportedGamesViewModel>(); // Singleton for ViewModel
        services.AddSingleton<HistoryViewModel>(); // Singleton for ViewModel
        services.AddSingleton<MainWindowViewModel>(); // Singleton for ViewModel
        services.AddTransient<MainWindow>(); // Transient for the UI window

        // Build the service provider
        ServiceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }
}