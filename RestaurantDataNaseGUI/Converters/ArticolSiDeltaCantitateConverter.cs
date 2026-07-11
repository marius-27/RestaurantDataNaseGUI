using System;
using System.Globalization;
using Avalonia.Data.Converters;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.ViewModels;

namespace RestaurantDataNaseGUI.Converters;

// Combina ArticolCosDto (binding) cu delta din ConverterParameter intr-un
// ModificaCantitateParametru, pentru butoanele +/- din CartView.
public class ArticolSiDeltaCantitateConverter : IValueConverter
{
    public static readonly ArticolSiDeltaCantitateConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ArticolCosDto articol || parameter is not string deltaText ||
            !decimal.TryParse(deltaText, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var delta))
        {
            return null;
        }

        return new ModificaCantitateParametru(articol, articol.Cantitate + delta);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
