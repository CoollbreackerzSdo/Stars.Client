namespace Stars.Client.Components;

/// <summary>Uppercase section label with an optional trailing hint.</summary>
public partial class SectionHeader : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(SectionHeader), string.Empty);

    public static readonly BindableProperty TrailingProperty =
        BindableProperty.Create(nameof(Trailing), typeof(string), typeof(SectionHeader), string.Empty,
            propertyChanged: (b, _, _) => ((SectionHeader)b).OnPropertyChanged(nameof(HasTrailing)));

    public SectionHeader()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Trailing
    {
        get => (string)GetValue(TrailingProperty);
        set => SetValue(TrailingProperty, value);
    }

    public bool HasTrailing => !string.IsNullOrWhiteSpace(Trailing);
}
