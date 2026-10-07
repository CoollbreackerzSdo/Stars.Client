using System.Collections;

namespace Stars.Client.Components;

/// <summary>Monospaced-looking activity feed inside an inset panel.</summary>
public partial class LogPanel : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(LogPanel), "ACTIVITY");

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(LogPanel), null);

    public static readonly BindableProperty ListHeightProperty =
        BindableProperty.Create(nameof(ListHeight), typeof(double), typeof(LogPanel), 190.0);

    public LogPanel()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public double ListHeight
    {
        get => (double)GetValue(ListHeightProperty);
        set => SetValue(ListHeightProperty, value);
    }
}
