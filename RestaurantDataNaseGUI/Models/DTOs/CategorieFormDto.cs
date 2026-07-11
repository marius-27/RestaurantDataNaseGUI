namespace RestaurantDataNaseGUI.Models.DTOs;

// Date formular creare/editare Categorie; Id = 0 = categorie noua.
public class CategorieFormDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
}
