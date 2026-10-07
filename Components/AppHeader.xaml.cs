namespace Stars.Client.Components;

/// <summary>Top brand bar with a signal accent and a live status word.</summary>
public partial class AppHeader : ContentView
{
    public static readonly BindableProperty SubtitleProperty =
        BindableProperty.Create(nameof(Subtitle), typeof(string), typeof(AppHeader), string.Empty);

    public static readonly BindableProperty StatusProperty =
        BindableProperty.Create(nameof(Status), typeof(string), typeof(AppHeader), string.Empty);

    public static readonly BindableProperty StatusColorProperty =
        BindableProperty.Create(nameof(StatusColor), typeof(Color), typeof(AppHeader), Color.FromArgb("#2DD4BF"));

    public AppHeader()
    {
        InitializeComponent();
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public Color StatusColor
    {
        get => (Color)GetValue(StatusColorProperty);
        set => SetValue(StatusColorProperty, value);
    }
}
