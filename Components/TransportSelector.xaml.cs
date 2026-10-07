using Stars.Shared;

namespace Stars.Client.Components;

public partial class TransportSelector : ContentView
{
    public static readonly BindableProperty SelectedKindProperty =
        BindableProperty.Create(nameof(SelectedKind), typeof(TransportKind), typeof(TransportSelector),
            TransportKind.Wifi, BindingMode.TwoWay, propertyChanged: OnSelectedKindChanged);

    public static readonly BindableProperty BluetoothSupportedProperty =
        BindableProperty.Create(nameof(BluetoothSupported), typeof(bool), typeof(TransportSelector),
            true, propertyChanged: OnBluetoothSupportedChanged);

    public TransportSelector()
    {
        InitializeComponent();
        UpdateVisuals();
    }

    public TransportKind SelectedKind
    {
        get => (TransportKind)GetValue(SelectedKindProperty);
        set => SetValue(SelectedKindProperty, value);
    }

    public bool BluetoothSupported
    {
        get => (bool)GetValue(BluetoothSupportedProperty);
        set => SetValue(BluetoothSupportedProperty, value);
    }

    private static void OnSelectedKindChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TransportSelector)bindable).UpdateVisuals();

    private static void OnBluetoothSupportedChanged(BindableObject bindable, object oldValue, object newValue)
        => ((TransportSelector)bindable).UpdateVisuals();

    private async void OnWifiTapped(object? sender, TappedEventArgs e) => await SelectAsync(TransportKind.Wifi);
    private async void OnUsbTapped(object? sender, TappedEventArgs e) => await SelectAsync(TransportKind.Usb);
    private async void OnBluetoothTapped(object? sender, TappedEventArgs e) => await SelectAsync(TransportKind.Bluetooth);

    private Task SelectAsync(TransportKind kind)
    {
        if (kind == TransportKind.Bluetooth && !BluetoothSupported)
            return Task.CompletedTask;

        SelectedKind = kind;
        return Task.CompletedTask;
    }

    private void UpdateVisuals()
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        ApplyOption(WifiOption, WifiLabel, SelectedKind == TransportKind.Wifi, true, dark);
        ApplyOption(UsbOption, UsbLabel, SelectedKind == TransportKind.Usb, true, dark);
        ApplyOption(BluetoothOption, BluetoothLabel, SelectedKind == TransportKind.Bluetooth,
            BluetoothSupported, dark);
    }

    private static void ApplyOption(Border border, Label label, bool selected, bool enabled, bool dark)
    {
        border.BackgroundColor = selected
            ? Color.FromArgb(dark ? "#B9DB78" : "#1F6B4E")
            : Color.FromArgb(dark ? "#29352C" : "#E8EFE9");
        border.Opacity = enabled ? 1 : 0.4;
        label.TextColor = selected
            ? dark ? Color.FromArgb("#17221A") : Colors.White
            : Color.FromArgb(dark ? "#DCE8DC" : "#34473A");
        label.FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular";
    }
}