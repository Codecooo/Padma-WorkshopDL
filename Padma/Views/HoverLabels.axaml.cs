using Avalonia;
using Avalonia.Controls.Primitives;
using Padma.Models;

namespace Padma.Views;

public class HoverLabels : TemplatedControl
{
    public static readonly StyledProperty<string> SmallTextProperty = AvaloniaProperty.Register<HoverLabels, string>(
        nameof(SmallText), "SMALL");

    private readonly LiteDbHistory _dbHistory;

    public HoverLabels()
    {
        _dbHistory = new LiteDbHistory();
    }

    public string SmallText
    {
        get => GetValue(SmallTextProperty);
        set => SetValue(SmallTextProperty, value);
    }
}