using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>Datele unui formular de creare/editare Preparat. Id = 0 inseamna preparat nou.</summary>
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

    /// <summary>Id-urile alergenilor selectati pentru acest preparat - inlocuiesc integral asocierile existente la Update.</summary>
    public List<int> AlergenIds { get; set; } = new();

    /// <summary>Caile de imagine ale preparatului - inlocuiesc integral imaginile existente la Update.</summary>
    public List<string> ImaginiPaths { get; set; } = new();
}
