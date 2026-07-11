namespace RestaurantDataNaseGUI.Models;

// O linie de comanda: fie Preparat, fie Meniu - niciodata ambele, niciodata
// niciunul (CK_ComandaDetaliu_PreparatSauMeniu).
public class ComandaDetaliu
{
    public int Id { get; set; }
    public int ComandaId { get; set; }
    public int? PreparatId { get; set; }
    public int? MeniuId { get; set; }
    public decimal Cantitate { get; set; }

    // Pretul la momentul plasarii comenzii (snapshot istoric).
    public decimal PretUnitarLaComanda { get; set; }

    public Comanda Comanda { get; set; } = null!;
    public Preparat? Preparat { get; set; }
    public Meniu? Meniu { get; set; }
}
