namespace RestaurantDataNaseGUI.Models;

/// <summary>
/// Entitate de legatura explicita pentru relatia many-to-many Meniu &lt;-&gt; Preparat,
/// cu atributul propriu CantitateInMeniu. Cheie primara compusa (MeniuId, PreparatId).
/// </summary>
public class MeniuPreparat
{
    public int MeniuId { get; set; }
    public int PreparatId { get; set; }
    public decimal CantitateInMeniu { get; set; }

    public Meniu Meniu { get; set; } = null!;
    public Preparat Preparat { get; set; } = null!;
}
