namespace RestaurantDataNaseGUI.Models.DTOs;

// Comanda vazuta de angajat - extinde ComandaClientDto cu datele clientului
// (nume, telefon, adresa). Vezi IOrderService.GetToateComenzileAsync/GetComenziActiveAngajatAsync.
public class ComandaAngajatDto : ComandaClientDto
{
    public string NumeClient { get; set; } = string.Empty;
    public string PrenumeClient { get; set; } = string.Empty;
    public string TelefonClient { get; set; } = string.Empty;
    public string? AdresaLivrareClient { get; set; }
}
