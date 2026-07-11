namespace RestaurantDataNaseGUI.Models;

// Entitate de legatura many-to-many Preparat <-> Alergen. Cheie primara compusa (PreparatId, AlergenId).
public class PreparatAlergen
{
    public int PreparatId { get; set; }
    public int AlergenId { get; set; }

    public Preparat Preparat { get; set; } = null!;
    public Alergen Alergen { get; set; } = null!;
}
