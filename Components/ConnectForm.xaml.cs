using System.Windows.Input;

namespace Stars.Client.Components;

public partial class ConnectForm : ContentView
{
    public static readonly BindableProperty HostProperty =
        BindableProperty.Create(nameof(Host), typeof(string), typeof(ConnectForm), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty PortProperty =
        BindableProperty.Create(nameof(Port), typeof(string), typeof(ConnectForm), string.Empty, BindingMode.TwoWay);

    public static readonly BindableProperty ConnectCommandProperty =
        BindableProperty.Create(nameof(ConnectCommand), typeof(ICommand), typeof(ConnectForm), null);

    public static readonly BindableProperty DiscoverCommandProperty =
        BindableProperty.Create(nameof(DiscoverCommand), typeof(ICommand), typeof(ConnectForm), null);

    public ConnectForm()
    {
        InitializeComponent();
    }

    public string Host
    {
        get => (string)GetValue(HostProperty);
        set => SetValue(HostProperty, value);
    }

    public string Port
    {
        get => (string)GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }

    public ICommand? ConnectCommand
    {
        get => (ICommand?)GetValue(ConnectCommandProperty);
        set => SetValue(ConnectCommandProperty, value);
    }

    public ICommand? DiscoverCommand
    {
        get => (ICommand?)GetValue(DiscoverCommandProperty);
        set => SetValue(DiscoverCommandProperty, value);
    }
}
