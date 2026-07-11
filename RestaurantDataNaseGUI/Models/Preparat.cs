using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Preparat
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public decimal Pret { get; set; }
    public decimal CantitatePortie { get; set; }

    // Unitatea de masura a portiei si a stocului, ex: "g", "ml", "buc".
    public string UnitateMasura { get; set; } = string.Empty;

    // Stocul curent, in aceeasi unitate de masura ca UnitateMasura.
    public decimal CantitateTotalaRestaurant { get; set; }

    public int CategorieId { get; set; }

    // Soft-delete: preparat deja folosit intr-o comanda nu se sterge fizic,
    // ci se marcheaza indisponibil (vezi dbo.sp_SetPreparatIndisponibil).
    public bool Disponibil { get; set; } = true;

    public Categorie Categorie { get; set; } = null!;
    public ICollection<PreparatImagine> Imagini { get; set; } = new List<PreparatImagine>();
    public ICollection<PreparatAlergen> PreparatAlergeni { get; set; } = new List<PreparatAlergen>();
    public ICollection<MeniuPreparat> MeniuPreparate { get; set; } = new List<MeniuPreparat>();
    public ICollection<ComandaDetaliu> ComandaDetalii { get; set; } = new List<ComandaDetaliu>();
}
