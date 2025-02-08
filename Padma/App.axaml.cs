    using System;
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
            try
            {
                var collection = new ServiceCollection();
                AddCommonServices(collection);

                // Build the service provider
                ServiceProvider = collection.BuildServiceProvider();

                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    DisableAvaloniaDataAnnotationValidation();
                    desktop.MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                }

                base.OnFrameworkInitializationCompleted();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static void AddCommonServices (IServiceCollection collection)
        {
            // Register services with DI container
            collection.AddTransient<MainWindow>(); // Transient for the UI window
            collection.AddSingleton<SupportedGames>(); // Singleton for database access
            collection.AddSingleton<SaveHistory>(); // Singleton for database access
            collection.AddSingleton<HomeViewModel>(); // Singleton for ViewModel
            collection.AddSingleton<SupportedGamesViewModel>(); // Singleton for ViewModel
            collection.AddSingleton<AppIdFinder>(); // Singleton for ViewModel
            collection.AddSingleton<CmdRunner>(); // Singleton for ViewModel
            collection.AddSingleton<ThumbnailLoader>(); // Singleton for ViewModel
            collection.AddSingleton<HistoryViewModel>(); // Singleton for ViewModel
            collection.AddSingleton<MainWindowViewModel>(); // Singleton for ViewModel
            collection.AddSingleton<SettingsViewModel>(); // Singleton for ViewModel
            collection.AddSingleton<DownloadProgressTracker>(); // Singleton for ViewModel
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
        }
    }