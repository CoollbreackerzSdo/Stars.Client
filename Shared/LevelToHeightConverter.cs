using System.Globalization;

namespace Stars.Client.Shared;

/// <summary>Scales the 0..1 audio level into a pixel height for the equalizer bars.</summary>
public sealed class LevelToHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value is double d ? d : 0;
        var factor = parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)
            ? f
            : 1.0;

        // Base + scaled height so bars always look alive.
        return 14 + level * 70 * factor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
