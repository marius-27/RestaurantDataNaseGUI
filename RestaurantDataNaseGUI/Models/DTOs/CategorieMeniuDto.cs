using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>O categorie din meniu, cu preparatele si meniurile aferente deja grupate.</summary>
public class CategorieMeniuDto
{
    public string Denumire { get; set; } = string.Empty;
    public IReadOnlyList<MeniuAfisareDto> Itemi { get; set; } = Array.Empty<MeniuAfisareDto>();
}
