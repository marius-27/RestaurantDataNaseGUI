namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>Rezultatul calculului de cost al unei comenzi (vezi IOrderService.CalculeazaCostComandaAsync).</summary>
public class CalculComandaDto
{
    /// <summary>Suma preparatelor/meniurilor din cos, inainte de discount si transport.</summary>
    public decimal SubtotalMancare { get; set; }

    /// <summary>Procentul de discount aplicat (0 daca nu se aplica niciun discount).</summary>
    public decimal ProcentDiscount { get; set; }

    /// <summary>Valoarea in lei a discountului (SubtotalMancare * ProcentDiscount / 100).</summary>
    public decimal ValoareDiscount { get; set; }

    /// <summary>
    /// Motivul discountului aplicat (comanda mare, client frecvent, sau
    /// ambele), pentru afisare - null daca ProcentDiscount e 0.
    /// </summary>
    public string? MotivDiscount { get; set; }

    public decimal CostTransport { get; set; }

    /// <summary>SubtotalMancare - ValoareDiscount + CostTransport.</summary>
    public decimal Total { get; set; }

    /// <summary>True daca s-a aplicat vreun discount - util pentru IsVisible in XAML fara convertor.</summary>
    public bool AreDiscount => ProcentDiscount > 0;
}
