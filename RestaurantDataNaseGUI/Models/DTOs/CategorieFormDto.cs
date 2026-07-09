namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>Datele unui formular de creare/editare Categorie. Id = 0 inseamna categorie noua.</summary>
public class CategorieFormDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
}
