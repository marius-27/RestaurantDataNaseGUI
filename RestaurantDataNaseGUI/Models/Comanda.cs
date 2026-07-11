using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Comanda
{
    public int Id { get; set; }
    public string CodUnic { get; set; } = string.Empty;
    public int UtilizatorId { get; set; }
    public DateTime DataComanda { get; set; }
    public int StareId { get; set; }
    public decimal CostTransport { get; set; }
    public decimal Discount { get; set; }
    public DateTime? OraEstimataLivrare { get; set; }

    public Utilizator Utilizator { get; set; } = null!;
    public StareComanda Stare { get; set; } = null!;
    public ICollection<ComandaDetaliu> ComandaDetalii { get; set; } = new List<ComandaDetaliu>();
}
