namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>
/// O comanda vazuta de un angajat - extinde ComandaClientDto cu datele
/// clientului care a plasat-o (nume, prenume, telefon, adresa de livrare),
/// necesare pentru vizualizarea/urmarirea comenzilor din partea
/// restaurantului. Vezi IOrderService.GetToateComenzileAsync/GetComenziActiveAngajatAsync.
/// </summary>
public class ComandaAngajatDto : ComandaClientDto
{
    public string NumeClient { get; set; } = string.Empty;
    public string PrenumeClient { get; set; } = string.Empty;
    public string TelefonClient { get; set; } = string.Empty;
    public string? AdresaLivrareClient { get; set; }
}
