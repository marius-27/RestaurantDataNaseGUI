namespace RestaurantDataNaseGUI.Models;

// Entitate de legatura many-to-many Meniu <-> Preparat, cu atributul propriu
// CantitateInMeniu. Cheie primara compusa (MeniuId, PreparatId).
public class MeniuPreparat
{
    public int MeniuId { get; set; }
    public int PreparatId { get; set; }
    public decimal CantitateInMeniu { get; set; }

    public Meniu Meniu { get; set; } = null!;
    public Preparat Preparat { get; set; } = null!;
}
