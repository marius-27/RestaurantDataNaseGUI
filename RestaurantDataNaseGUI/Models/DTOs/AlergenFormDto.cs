namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>Datele unui formular de creare/editare Alergen. Id = 0 inseamna alergen nou.</summary>
public class AlergenFormDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
}
