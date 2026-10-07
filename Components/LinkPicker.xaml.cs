using Stars.Shared;

namespace Stars.Client.Components;

/// <summary>Segmented picker for the transport (Wi-Fi / USB / Bluetooth).</summary>
public partial class LinkPicker : ContentView
{
    public static readonly BindableProperty SelectedKindProperty =
        BindableProperty.Create(nameof(SelectedKind), typeof(TransportKind), typeof(LinkPicker),
            TransportKind.Wifi, BindingMode.TwoWay, propertyChanged: OnKindChanged);

    public static readonly BindableProperty BluetoothSupportedProperty =
        BindableProperty.Create(nameof(BluetoothSupported), typeof(bool), typeof(LinkPicker), true,
            propertyChanged: OnBluetoothChanged);

    private static readonly Color SelectedBg = Color.FromArgb("#2DD4BF");
    private static readonly Color UnselectedBg = Color.FromArgb("#332DD4BF");

    public LinkPicker()
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

    private static void OnKindChanged(BindableObject b, object o, object n) => ((LinkPicker)b).UpdateVisuals();
    private static void OnBluetoothChanged(BindableObject b, object o, object n) => ((LinkPicker)b).UpdateVisuals();

    private void OnWifiTapped(object? sender, TappedEventArgs e) => Select(TransportKind.Wifi);
    private void OnUsbTapped(object? sender, TappedEventArgs e) => Select(TransportKind.Usb);
    private void OnBluetoothTapped(object? sender, TappedEventArgs e) => Select(TransportKind.Bluetooth);

    private void Select(TransportKind kind)
    {
        if (kind == TransportKind.Bluetooth && !BluetoothSupported)
            return;

        SelectedKind = kind;
    }

    private void UpdateVisuals()
    {
        Apply(WifiOption, WifiLabel, SelectedKind == TransportKind.Wifi, true);
        Apply(UsbOption, UsbLabel, SelectedKind == TransportKind.Usb, true);
        Apply(BluetoothOption, BluetoothLabel, SelectedKind == TransportKind.Bluetooth, BluetoothSupported);
    }

    private static void Apply(Border border, Label label, bool selected, bool enabled)
    {
        border.BackgroundColor = selected ? SelectedBg : UnselectedBg;
        border.Opacity = enabled ? 1 : 0.4;
        label.TextColor = selected ? Color.FromArgb("#04211C") : Color.FromArgb("#E8EDF6");
        label.FontFamily = selected ? "OpenSansSemibold" : "OpenSansRegular";
    }
}
