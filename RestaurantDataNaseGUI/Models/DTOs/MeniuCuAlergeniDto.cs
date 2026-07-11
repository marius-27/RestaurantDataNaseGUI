namespace RestaurantDataNaseGUI.Models.DTOs;

// Linie din dbo.sp_GetMeniuRestaurantCuAlergeni; tip keyless, doar pentru FromSqlInterpolated.
public class MeniuCuAlergeniDto
{
    public int MeniuId { get; set; }
    public string Meniu { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal PretCalculat { get; set; }

    // Alergeni separati prin ", "; null daca meniul nu are componente cu alergeni.
    public string? Alergeni { get; set; }
}
