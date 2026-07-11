using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

// O categorie din meniu, cu preparatele/meniurile deja grupate.
public class CategorieMeniuDto
{
    public string Denumire { get; set; } = string.Empty;
    public IReadOnlyList<MeniuAfisareDto> Itemi { get; set; } = Array.Empty<MeniuAfisareDto>();
}
