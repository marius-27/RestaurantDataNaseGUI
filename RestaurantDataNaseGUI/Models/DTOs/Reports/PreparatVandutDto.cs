namespace RestaurantDataNaseGUI.Models.DTOs.Reports;

/// <summary>
/// O linie din IReportService.RaportPreparateCelMaiVanduteAsync - un
/// preparat sau meniu, cu cantitatea totala comandata in perioada (comenzile
/// anulate sunt excluse). SumaIncasata e suma bruta a liniilor
/// (Cantitate * PretUnitarLaComanda), fara a proratiza discountul aplicat la
/// nivel de comanda intreaga - vezi README pentru justificare.
/// </summary>
public class PreparatVandutDto
{
    public string Denumire { get; set; } = string.Empty;

    /// <summary>"Preparat" sau "Meniu".</summary>
    public string Tip { get; set; } = string.Empty;

    public string Categorie { get; set; } = string.Empty;
    public decimal CantitateTotalaComandata { get; set; }
    public decimal SumaIncasata { get; set; }
}
