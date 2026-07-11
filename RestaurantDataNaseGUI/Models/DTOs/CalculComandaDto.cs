namespace RestaurantDataNaseGUI.Models.DTOs;

// Rezultatul calculului de cost al unei comenzi (IOrderService.CalculeazaCostComandaAsync).
public class CalculComandaDto
{
    // Suma preparatelor/meniurilor din cos, fara discount/transport.
    public decimal SubtotalMancare { get; set; }

    // Procentul de discount aplicat (0 = fara discount).
    public decimal ProcentDiscount { get; set; }

    // Valoarea discountului in lei (SubtotalMancare * ProcentDiscount / 100).
    public decimal ValoareDiscount { get; set; }

    // Motivul discountului (comanda mare/client frecvent/ambele), pentru
    // afisare; null daca ProcentDiscount e 0.
    public string? MotivDiscount { get; set; }

    public decimal CostTransport { get; set; }

    // SubtotalMancare - ValoareDiscount + CostTransport.
    public decimal Total { get; set; }

    // True daca exista discount; util pentru IsVisible in XAML fara convertor.
    public bool AreDiscount => ProcentDiscount > 0;
}
