namespace RestaurantDataNaseGUI.Models.DTOs.Reports;

// Linie din IReportService.RaportPreparateCelMaiVanduteAsync: preparat/meniu cu cantitatea
// comandata in perioada (comenzi anulate excluse). SumaIncasata e bruta (Cantitate *
// PretUnitarLaComanda), fara proratizarea discountului de comanda - vezi README.
public class PreparatVandutDto
{
    public string Denumire { get; set; } = string.Empty;

    // "Preparat" sau "Meniu".
    public string Tip { get; set; } = string.Empty;

    public string Categorie { get; set; } = string.Empty;
    public decimal CantitateTotalaComandata { get; set; }
    public decimal SumaIncasata { get; set; }
}
