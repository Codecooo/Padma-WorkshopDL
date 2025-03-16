using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Padma.ViewModels;
using Padma.Views;

namespace Padma;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        if (param is HomeViewModel)
            return new HomeViews();
        if (param is MainWindowViewModel)
            return new MainWindow();
        if (param is SettingsViewModel)
            return new SettingsViews();
        if (param is SupportedGamesViewModel)
            return new SupportedGamesViews();
        if(param is HistoryViewModel)
            return new HistoryViews();
        
        return new TextBlock { Text = $"Not Found: {param.GetType().FullName}" };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}