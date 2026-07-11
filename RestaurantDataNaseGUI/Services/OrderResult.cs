using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// Rezultatul unei operatii de creare a unei comenzi din IOrderService.
public sealed class OrderResult
{
    public bool Succes { get; }
    public int? ComandaId { get; }
    public string? CodUnic { get; }
    public CalculComandaDto? Calcul { get; }
    public string? MesajEroare { get; }

    private OrderResult(bool succes, int? comandaId, string? codUnic, CalculComandaDto? calcul, string? mesajEroare)
    {
        Succes = succes;
        ComandaId = comandaId;
        CodUnic = codUnic;
        Calcul = calcul;
        MesajEroare = mesajEroare;
    }

    // calcul e null pentru operatii fara calcul de cost (ex. anulare comanda).
    public static OrderResult Ok(int comandaId, string codUnic, CalculComandaDto? calcul = null) =>
        new(true, comandaId, codUnic, calcul, null);

    public static OrderResult Esec(string mesajEroare) => new(false, null, null, null, mesajEroare);
}
