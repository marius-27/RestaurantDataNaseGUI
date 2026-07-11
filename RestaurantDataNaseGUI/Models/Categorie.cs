using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Categorie
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;

    public ICollection<Preparat> Preparate { get; set; } = new List<Preparat>();
    public ICollection<Meniu> Meniuri { get; set; } = new List<Meniu>();
}
