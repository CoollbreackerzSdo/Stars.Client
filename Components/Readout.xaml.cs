namespace Stars.Client.Components;

/// <summary>A labelled metric with an accent bar — the "instrument readout" of the Signal UI.</summary>
public partial class Readout : ContentView
{
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(Readout), string.Empty);

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(string), typeof(Readout), string.Empty);

    public static readonly BindableProperty HintProperty =
        BindableProperty.Create(nameof(Hint), typeof(string), typeof(Readout), string.Empty,
            propertyChanged: (b, _, _) => ((Readout)b).OnPropertyChanged(nameof(HasHint)));

    public static readonly BindableProperty AccentProperty =
        BindableProperty.Create(nameof(Accent), typeof(Color), typeof(Readout), Color.FromArgb("#2DD4BF"));

    public Readout()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public Color Accent
    {
        get => (Color)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public bool HasHint => !string.IsNullOrWhiteSpace(Hint);
}
