using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Padma.Models;
using Padma.Services;
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
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            // Create a single instance of MainWindow
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<DownloadProgressTracker>();
        services.AddSingleton<SupportedGames>();
        services.AddSingleton<SaveHistory>();
        services.AddSingleton<CmdRunner>();
        services.AddSingleton<ThumbnailLoader>();
        services.AddSingleton<FolderPicker>();
        services.AddSingleton<StellarisAutoInstall>();
        services.AddSingleton<DownloadMods>();
        services.AddSingleton<DownloadProcessor>();

        // Register AppIdFinder after DownloadProgressTracker
        services.AddSingleton<AppIdFinder>();

        // Register ViewModels after services
        services.AddSingleton<SupportedGamesViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Register MainWindowViewModel last
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }

    [RequiresUnreferencedCode("Calls Avalonia.Data.Core.Plugins.BindingPlugins.DataValidators")]
    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }
}