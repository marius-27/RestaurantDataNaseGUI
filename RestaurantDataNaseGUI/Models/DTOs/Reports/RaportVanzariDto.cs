using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs.Reports;

/// <summary>O zi din defalcarea pe zile a RaportVanzariDto.</summary>
public class VanzareZilnicaDto
{
    public DateTime Data { get; set; }

    /// <summary>Toate comenzile plasate in aceasta zi, indiferent de stare.</summary>
    public int NumarComenzi { get; set; }

    /// <summary>Dintre NumarComenzi, cate au fost anulate.</summary>
    public int NumarComenziAnulate { get; set; }

    /// <summary>Suma incasata in aceasta zi (exclude comenzile anulate).</summary>
    public decimal SumaIncasata { get; set; }
}

/// <summary>
/// Rezultatul IReportService.RaportVanzariPerioadaAsync: numarul de comenzi,
/// suma totala incasata (mancare + transport - discount, exclude comenzile
/// anulate) si numarul de comenzi anulate, pentru un interval [DataStart,
/// DataEnd], plus defalcarea pe zile.
/// </summary>
public class RaportVanzariDto
{
    public DateTime DataStart { get; set; }
    public DateTime DataEnd { get; set; }

    /// <summary>Toate comenzile plasate in perioada, indiferent de stare.</summary>
    public int NumarComenzi { get; set; }

    public int NumarComenziAnulate { get; set; }

    /// <summary>Suma incasata in perioada (exclude comenzile anulate).</summary>
    public decimal SumaTotalaIncasata { get; set; }

    public List<VanzareZilnicaDto> Zile { get; set; } = new();
}
