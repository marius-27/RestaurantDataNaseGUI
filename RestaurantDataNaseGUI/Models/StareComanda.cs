using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class StareComanda
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;

    public ICollection<Comanda> Comenzi { get; set; } = new List<Comanda>();
}
