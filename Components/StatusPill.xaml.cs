namespace Stars.Client.Components;

/// <summary>Pill-shaped status indicator with a leading dot.</summary>
public partial class StatusPill : ContentView
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(StatusPill), string.Empty);

    public static readonly BindableProperty PillColorProperty =
        BindableProperty.Create(nameof(PillColor), typeof(Color), typeof(StatusPill), Color.FromArgb("#2DD4BF"));

    public StatusPill()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Color PillColor
    {
        get => (Color)GetValue(PillColorProperty);
        set => SetValue(PillColorProperty, value);
    }
}
