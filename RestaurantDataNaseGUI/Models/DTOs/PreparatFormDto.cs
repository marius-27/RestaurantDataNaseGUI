using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

// Formular de creare/editare Preparat. Id = 0 inseamna preparat nou.
public class PreparatFormDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public decimal Pret { get; set; }
    public decimal CantitatePortie { get; set; }
    public string UnitateMasura { get; set; } = string.Empty;
    public decimal CantitateTotalaRestaurant { get; set; }
    public int CategorieId { get; set; }
    public bool Disponibil { get; set; } = true;

    // Id-uri alergeni selectati - inlocuiesc integral asocierile existente la Update.
    public List<int> AlergenIds { get; set; } = new();

    // Caile de imagine ale preparatului - inlocuiesc integral imaginile existente la Update.
    public List<string> ImaginiPaths { get; set; } = new();
}
