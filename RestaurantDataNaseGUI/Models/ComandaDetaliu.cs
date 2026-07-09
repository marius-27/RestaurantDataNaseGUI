namespace RestaurantDataNaseGUI.Models;

/// <summary>
/// O linie de comanda. Se refera fie la un Preparat, fie la un Meniu -
/// niciodata la ambele si niciodata la niciunul (vezi
/// CK_ComandaDetaliu_PreparatSauMeniu din schema.sql).
/// </summary>
public class ComandaDetaliu
{
    public int Id { get; set; }
    public int ComandaId { get; set; }
    public int? PreparatId { get; set; }
    public int? MeniuId { get; set; }
    public decimal Cantitate { get; set; }

    /// <summary>Snapshot istoric al pretului la momentul plasarii comenzii.</summary>
    public decimal PretUnitarLaComanda { get; set; }

    public Comanda Comanda { get; set; } = null!;
    public Preparat? Preparat { get; set; }
    public Meniu? Meniu { get; set; }
}
