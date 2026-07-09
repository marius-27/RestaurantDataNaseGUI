namespace RestaurantDataNaseGUI.Models;

/// <summary>
/// Tabel generic cheie-valoare pentru parametrii aplicatiei (ex. procente de
/// discount, praguri de stoc). Toate valorile trebuie citite din acest tabel
/// la runtime, niciodata hardcodate in cod C#.
/// </summary>
public class Configurare
{
    public int Id { get; set; }
    public string Cheie { get; set; } = string.Empty;
    public string Valoare { get; set; } = string.Empty;
    public string? Descriere { get; set; }
}
