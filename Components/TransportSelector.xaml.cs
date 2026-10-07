using Stars.Shared;

namespace Stars.Client.Components;

public partial class TransportSelector : ContentView
{
    public static readonly BindableProperty SelectedKindProperty =
        BindableProperty.Create(
            nameof(SelectedKind),
            typeof(TransportKind),
            typeof(TransportSelector),
            TransportKind.Wifi,
            BindingMode.TwoWay,
            propertyChanged: OnSelectedKindChanged);

    public static readonly BindableProperty BluetoothSupportedProperty =
        BindableProperty.Create(
            nameof(BluetoothSupported),
            typeof(bool),
            typeof(TransportSelector),
            true,
            propertyChanged: OnBluetoothSupportedChanged);

    private static readonly Color SelectedColor = Color.FromArgb("#4EA8FF");
    private static readonly Color UnselectedColor = Color.FromArgb("#2A7C5CFF");
    private static readonly Color UnselectedText = Color.FromArgb("#8A6BFF");

    public TransportSelector()
    {
        InitializeComponent();
        UpdateVisuals();
    }

    /// <summary>The transport the user has selected.</summary>
    public TransportKind SelectedKind
    {
        get => (TransportKind)GetValue(SelectedKindProperty);
        set => SetValue(SelectedKindProperty, value);
    }

    /// <summary>Whether the Bluetooth option should be selectable.</summary>
    public bool BluetoothSupported
    {
        get => (bool)GetValue(BluetoothSupportedProperty);
        set => SetValue(BluetoothSupportedProperty, value);
    }

    private static void OnSelectedKindChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TransportSelector)bindable).UpdateVisuals();

    private static void OnBluetoothSupportedChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TransportSelector)bindable).UpdateVisuals();

    private async void OnWifiTapped(object? sender, TappedEventArgs e) => await SelectAsync(TransportKind.Wifi, WifiOption);
    private async void OnUsbTapped(object? sender, TappedEventArgs e) => await SelectAsync(TransportKind.Usb, UsbOption);
    private async void OnBluetoothTapped(object? sender, TappedEventArgs e) => await SelectAsync(TransportKind.Bluetooth, BluetoothOption);

    private async Task SelectAsync(TransportKind kind, VisualElement option)
    {
        if (kind == TransportKind.Bluetooth && !BluetoothSupported)
            return;

        SelectedKind = kind;
        await option.PopAsync();
    }

    private void UpdateVisuals()
    {
        ApplyOption(WifiOption, WifiLabel, SelectedKind == TransportKind.Wifi, true);
        ApplyOption(UsbOption, UsbLabel, SelectedKind == TransportKind.Usb, true);
        ApplyOption(BluetoothOption, BluetoothLabel, SelectedKind == TransportKind.Bluetooth, BluetoothSupported);
    }

    private static void ApplyOption(Border border, Label label, bool selected, bool enabled)
    {
        border.BackgroundColor = selected ? SelectedColor : UnselectedColor;
        border.Opacity = enabled ? 1 : 0.4;
        label.TextColor = selected ? Colors.White : UnselectedText;
        label.FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular";
    }
}
