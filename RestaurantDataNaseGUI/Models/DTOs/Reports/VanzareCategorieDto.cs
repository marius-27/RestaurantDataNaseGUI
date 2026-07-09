namespace RestaurantDataNaseGUI.Models.DTOs.Reports;

/// <summary>
/// O linie din IReportService.RaportVanzariPeCategorieAsync - suma vanzarilor
/// (comenzi anulate excluse) grupata pe categoria preparatelor/meniurilor
/// comandate. SumaIncasata e bruta, fara proratizarea discountului de comanda
/// - vezi README pentru justificare.
/// </summary>
public class VanzareCategorieDto
{
    public string Categorie { get; set; } = string.Empty;
    public decimal CantitateTotala { get; set; }
    public decimal SumaIncasata { get; set; }
}
