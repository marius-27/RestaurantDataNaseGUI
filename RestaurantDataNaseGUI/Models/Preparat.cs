using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Preparat
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public decimal Pret { get; set; }
    public decimal CantitatePortie { get; set; }

    /// <summary>Unitatea de masura a portiei si a stocului, ex: "g", "ml", "buc".</summary>
    public string UnitateMasura { get; set; } = string.Empty;

    /// <summary>Stocul curent, in aceeasi unitate de masura ca <see cref="UnitateMasura"/>.</summary>
    public decimal CantitateTotalaRestaurant { get; set; }

    public int CategorieId { get; set; }

    /// <summary>
    /// Soft-delete: un preparat folosit deja intr-o comanda nu se sterge fizic,
    /// ci se marcheaza indisponibil (vezi dbo.sp_SetPreparatIndisponibil).
    /// </summary>
    public bool Disponibil { get; set; } = true;

    public Categorie Categorie { get; set; } = null!;
    public ICollection<PreparatImagine> Imagini { get; set; } = new List<PreparatImagine>();
    public ICollection<PreparatAlergen> PreparatAlergeni { get; set; } = new List<PreparatAlergen>();
    public ICollection<MeniuPreparat> MeniuPreparate { get; set; } = new List<MeniuPreparat>();
    public ICollection<ComandaDetaliu> ComandaDetalii { get; set; } = new List<ComandaDetaliu>();
}
