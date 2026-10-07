namespace Stars.Client.Components;

/// <summary>Compact seven-bar level meter for the Signal UI.</summary>
public partial class WaveMeter : ContentView
{
    public static readonly BindableProperty LevelProperty =
        BindableProperty.Create(nameof(Level), typeof(double), typeof(WaveMeter), 0.0);

    public WaveMeter()
    {
        InitializeComponent();
    }

    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }
}
