using System;

namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>
/// O linie din rezultatul dbo.sp_GetComenziClientCuDetalii - o comanda cu
/// starea ei si o singura linie de detaliu (Preparat sau Meniu). Tip
/// "keyless" folosit doar cu FromSqlInterpolated, nu este o entitate cu
/// echivalent 1:1 intr-un tabel.
/// </summary>
public class ComenziClientDetaliuDto
{
    public int ComandaId { get; set; }
    public string CodUnic { get; set; } = string.Empty;
    public DateTime DataComanda { get; set; }
    public string Stare { get; set; } = string.Empty;
    public decimal CostTransport { get; set; }
    public decimal Discount { get; set; }
    public DateTime? OraEstimataLivrare { get; set; }
    public int DetaliuId { get; set; }

    /// <summary>"Preparat" sau "Meniu".</summary>
    public string TipArticol { get; set; } = string.Empty;
    public string DenumireArticol { get; set; } = string.Empty;
    public decimal Cantitate { get; set; }
    public decimal PretUnitarLaComanda { get; set; }
    public decimal SubTotal { get; set; }
}
