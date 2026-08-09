using System.Globalization;
using Avalonia.Data.Converters;
using DuneEdit.Core;

namespace DuneEdit.Desktop.ViewModels;

public sealed class TroopOccupationDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        TroopOccupation occupation => occupation.ToDisplayName(),
        TroopJob job => job.ToDisplayName(),
        TroopAllegiance allegiance => allegiance.ToDisplayName(),
        _ => null,
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
