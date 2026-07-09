using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>O componenta (preparat + cantitate) a unui meniu, in formularul de creare/editare.</summary>
public class MeniuPreparatFormDto
{
    public int PreparatId { get; set; }
    public decimal CantitateInMeniu { get; set; }
}

/// <summary>Datele unui formular de creare/editare Meniu. Id = 0 inseamna meniu nou.</summary>
public class MeniuFormDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public int CategorieId { get; set; }

    /// <summary>Preparatele componente - inlocuiesc integral componentele existente la Update.</summary>
    public List<MeniuPreparatFormDto> Preparate { get; set; } = new();
}
