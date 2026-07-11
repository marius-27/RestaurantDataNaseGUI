namespace RestaurantDataNaseGUI.Models.DTOs.Reports;

// Linie din IReportService.RaportStocCurentAsync: stocul curent, sortat pe categorie apoi denumire pentru afisare grupata.
public class PreparatStocDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal CantitateTotalaRestaurant { get; set; }
    public string UnitateMasura { get; set; } = string.Empty;
    public bool Disponibil { get; set; }
}
