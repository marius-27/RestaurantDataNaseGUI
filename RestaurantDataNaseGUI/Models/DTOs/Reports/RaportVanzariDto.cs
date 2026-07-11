using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs.Reports;

// O zi din defalcarea pe zile a RaportVanzariDto.
public class VanzareZilnicaDto
{
    public DateTime Data { get; set; }

    // Toate comenzile plasate in aceasta zi, indiferent de stare.
    public int NumarComenzi { get; set; }

    // Dintre NumarComenzi, cate au fost anulate.
    public int NumarComenziAnulate { get; set; }

    // Suma incasata in aceasta zi (exclude comenzile anulate).
    public decimal SumaIncasata { get; set; }
}

// Rezultatul IReportService.RaportVanzariPerioadaAsync pentru [DataStart, DataEnd]:
// numar comenzi, suma incasata (mancare + transport - discount, fara cele anulate),
// numar comenzi anulate, plus defalcarea pe zile.
public class RaportVanzariDto
{
    public DateTime DataStart { get; set; }
    public DateTime DataEnd { get; set; }

    // Toate comenzile plasate in perioada, indiferent de stare.
    public int NumarComenzi { get; set; }

    public int NumarComenziAnulate { get; set; }

    // Suma incasata in perioada (exclude comenzile anulate).
    public decimal SumaTotalaIncasata { get; set; }

    public List<VanzareZilnicaDto> Zile { get; set; } = new();
}
