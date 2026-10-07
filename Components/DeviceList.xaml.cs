using System.Collections;
using Stars.Client.Features.Discovery;

namespace Stars.Client.Components;

/// <summary>Selectable list of discovered servers/devices.</summary>
public partial class DeviceList : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(DeviceList), "SERVIDORES");

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(DeviceList), null);

    public static readonly BindableProperty ListHeightProperty =
        BindableProperty.Create(nameof(ListHeight), typeof(double), typeof(DeviceList), 120.0);

    public DeviceList()
    {
        InitializeComponent();
    }

    public event EventHandler<DiscoveredServer>? ServerSelected;

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

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is DiscoveredServer server)
            ServerSelected?.Invoke(this, server);

        if (sender is CollectionView view)
            view.SelectedItem = null;
    }
}
