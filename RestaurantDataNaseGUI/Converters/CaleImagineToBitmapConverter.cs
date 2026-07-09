using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace RestaurantDataNaseGUI.Converters;

/// <summary>
/// Incarca un Bitmap dintr-o cale de fisier (PreparatImagine.CalePoza). Caile
/// relative sunt rezolvate fata de directorul aplicatiei. Daca fisierul nu
/// exista sau nu poate fi incarcat ca imagine, returneaza null - View-ul
/// afiseaza un placeholder in acest caz, in loc sa arunce o exceptie.
/// </summary>
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
