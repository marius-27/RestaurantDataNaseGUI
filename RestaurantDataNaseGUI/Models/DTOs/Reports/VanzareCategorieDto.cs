namespace RestaurantDataNaseGUI.Models.DTOs.Reports;

// Linie din IReportService.RaportVanzariPeCategorieAsync: vanzari (fara cele anulate)
// grupate pe categorie. SumaIncasata e bruta, fara proratizarea discountului - vezi README.
public class VanzareCategorieDto
{
    public string Categorie { get; set; } = string.Empty;
    public decimal CantitateTotala { get; set; }
    public decimal SumaIncasata { get; set; }
}
