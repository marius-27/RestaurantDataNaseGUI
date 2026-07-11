namespace RestaurantDataNaseGUI.Models;

// Tabel cheie-valoare pentru parametrii aplicatiei (discount, praguri stoc
// etc.) - valorile se citesc din DB la runtime, niciodata hardcodate in C#.
public class Configurare
{
    public int Id { get; set; }
    public string Cheie { get; set; } = string.Empty;
    public string Valoare { get; set; } = string.Empty;
    public string? Descriere { get; set; }
}
