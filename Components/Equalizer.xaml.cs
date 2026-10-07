namespace Stars.Client.Components;

public partial class Equalizer : ContentView
{
    public static readonly BindableProperty LevelProperty =
        BindableProperty.Create(nameof(Level), typeof(double), typeof(Equalizer), 0.0);

    public Equalizer()
    {
        InitializeComponent();
    }

    /// <summary>Current pseudo audio level, 0..1.</summary>
    public double Level
    {
        get => (double)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }
}
