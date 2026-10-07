using System.Globalization;

namespace Stars.Client.Shared;

public sealed class LevelToHeightConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var level = value is double current ? Math.Clamp(current, 0, 1) : 0;
        var scale = parameter is string text && double.TryParse(text, NumberStyles.Float,
            CultureInfo.InvariantCulture, out var parsed) ? parsed : 1;
        return 8 + level * 80 * scale;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}