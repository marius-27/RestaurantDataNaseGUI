namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>
/// O linie din rezultatul dbo.sp_GetMeniuRestaurantCuAlergeni. Tip
/// "keyless" folosit doar cu FromSqlInterpolated.
/// </summary>
public class MeniuCuAlergeniDto
{
    public int MeniuId { get; set; }
    public string Meniu { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal PretCalculat { get; set; }

    /// <summary>Lista de alergeni, separati prin ", ". Null daca meniul nu are componente cu alergeni.</summary>
    public string? Alergeni { get; set; }
}
