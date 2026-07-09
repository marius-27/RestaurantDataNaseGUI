using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Alergen
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;

    public ICollection<PreparatAlergen> PreparatAlergeni { get; set; } = new List<PreparatAlergen>();
}
