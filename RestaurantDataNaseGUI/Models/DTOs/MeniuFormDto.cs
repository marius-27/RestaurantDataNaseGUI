using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

// O componenta (preparat + cantitate) a unui meniu, in formularul de creare/editare.
public class MeniuPreparatFormDto
{
    public int PreparatId { get; set; }
    public decimal CantitateInMeniu { get; set; }
}

// Formular de creare/editare Meniu. Id = 0 inseamna meniu nou.
public class MeniuFormDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public int CategorieId { get; set; }

    // Preparatele componente - inlocuiesc integral componentele existente la Update.
    public List<MeniuPreparatFormDto> Preparate { get; set; } = new();
}
