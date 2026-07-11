namespace RestaurantDataNaseGUI.Models.DTOs;

// Linie din dbo.sp_GetPreparateApropiateDeEpuizare; tip keyless, doar pentru FromSqlInterpolated.
public class PreparatEpuizareDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal CantitateTotalaRestaurant { get; set; }
    public string UnitateMasura { get; set; } = string.Empty;
    public bool Disponibil { get; set; }
}
