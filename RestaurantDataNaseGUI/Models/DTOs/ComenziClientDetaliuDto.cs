using System;

namespace RestaurantDataNaseGUI.Models.DTOs;

// O linie din dbo.sp_GetComenziClientCuDetalii: comanda + o linie de detaliu
// (Preparat/Meniu). Tip keyless, folosit doar cu FromSqlInterpolated.
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

    // "Preparat" sau "Meniu".
    public string TipArticol { get; set; } = string.Empty;
    public string DenumireArticol { get; set; } = string.Empty;
    public decimal Cantitate { get; set; }
    public decimal PretUnitarLaComanda { get; set; }
    public decimal SubTotal { get; set; }
}
