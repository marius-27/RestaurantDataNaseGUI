using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace RestaurantDataNaseGUI.Converters;

// Incarca un Bitmap din CalePoza (caile relative sunt fata de directorul
// aplicatiei); returneaza null daca fisierul lipseste/e invalid, ca View-ul
// sa afiseze un placeholder in loc sa arunce o exceptie.
public class CaleImagineToBitmapConverter : IValueConverter
{
    public static readonly CaleImagineToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string cale || string.IsNullOrWhiteSpace(cale))
        {
            return null;
        }

        var caleCompleta = Path.IsPathRooted(cale)
            ? cale
            : Path.Combine(AppContext.BaseDirectory, cale);

        if (!File.Exists(caleCompleta))
        {
            return null;
        }

        try
        {
            return new Bitmap(caleCompleta);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
