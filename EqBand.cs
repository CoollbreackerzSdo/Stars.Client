using CommunityToolkit.Mvvm.ComponentModel;

namespace Stars.Client;

/// <summary>One adjustable equalizer band shown as a slider in the UI.</summary>
public sealed class EqBand : ObservableObject
{
    private readonly Action<int, int> _onChanged;

    public EqBand(int index, string label, Action<int, int> onChanged)
    {
        Index = index;
        Label = label;
        _onChanged = onChanged;
    }

    public int Index { get; }

    public string Label { get; }

    private int _gainDb;

    public int GainDb
    {
        get => _gainDb;
        set
        {
            var clamped = Math.Clamp(value, -12, 12);
            if (SetProperty(ref _gainDb, clamped))
            {
                OnPropertyChanged(nameof(GainLabel));
                _onChanged(Index, clamped);
            }
        }
    }

    public string GainLabel => $"{_gainDb:+0;-0;0} dB";
}
