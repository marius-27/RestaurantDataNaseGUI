namespace RestaurantDataNaseGUI.Models;

/// <summary>
/// Entitate de legatura explicita pentru relatia many-to-many Preparat &lt;-&gt; Alergen.
/// Cheie primara compusa (PreparatId, AlergenId).
/// </summary>
public class PreparatAlergen
{
    public int PreparatId { get; set; }
    public int AlergenId { get; set; }

    public Preparat Preparat { get; set; } = null!;
    public Alergen Alergen { get; set; } = null!;
}
