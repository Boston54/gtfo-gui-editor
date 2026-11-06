using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using GTFO_GUIEditor.Models;

namespace GTFO_GUIEditor.Converters;

public class EventTypeColorConverter : IValueConverter
{
    public static readonly EventTypeColorConverter Instance = new();

    private static readonly IBrush AwoBrush = new SolidColorBrush(Color.Parse("#87CEEB")); // Light Sky Blue
    private static readonly IBrush DefaultBrush = new SolidColorBrush(Color.Parse("#E0E0E0"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is EventType evType && (int)evType >= 10000)
        {
            return AwoBrush;
        }
        return DefaultBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
