namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>
/// O linie din rezultatul dbo.sp_GetPreparateApropiateDeEpuizare. Tip
/// "keyless" folosit doar cu FromSqlInterpolated.
/// </summary>
public class PreparatEpuizareDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal CantitateTotalaRestaurant { get; set; }
    public string UnitateMasura { get; set; } = string.Empty;
    public bool Disponibil { get; set; }
}
