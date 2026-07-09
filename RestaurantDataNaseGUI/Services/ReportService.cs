using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models.DTOs.Reports;

namespace RestaurantDataNaseGUI.Services;

/// <summary>
/// Implementare IReportService. Toate rapoartele sunt EF Core LINQ pur:
/// filtrarea pe interval de date si sumele per-comanda/per-linie (proiectate
/// prin Select) sunt translatate de provider in SQL parametrizat; gruparea
/// finala (pe zi/preparat/categorie) se face cu GroupBy peste setul deja
/// filtrat, in memorie - acelasi tipar folosit deja in
/// OrderService.GetComenziClientAsync (grupare in memorie dupa incarcare).
///
/// "Comanda anulata" e determinata prin Comanda.Stare.Denumire == "anulata"
/// (nu exista o coloana booleana dedicata - vezi Comanda.cs), la fel ca
/// StariFinale din OrderService, dar aici ne intereseaza strict anularea, nu
/// si livrarea, deci setul e mai restrans (doar "anulata").
/// </summary>
public class ReportService : IReportService
{
    private static readonly HashSet<string> StariAnulate = new(StringComparer.OrdinalIgnoreCase)
    {
        "anulata",
    };

    private readonly ISessionService _sessionService;
    private readonly Func<RestaurantDbContext> _dbContextFactory;

    public ReportService(ISessionService? sessionService = null, Func<RestaurantDbContext>? dbContextFactory = null)
    {
        _sessionService = sessionService ?? SessionService.Instance;
        _dbContextFactory = dbContextFactory ?? (() => DatabaseConfig.CreateDbContext());
    }

    public async Task<RaportVanzariDto> RaportVanzariPerioadaAsync(
        DateTime dataStart,
        DateTime dataEnd,
        CancellationToken cancellationToken = default)
    {
        VerificaEsteAngajatSauArunca();

        var (start, sfarsitExclusiv) = NormalizeazaInterval(dataStart, dataEnd);

        await using var context = _dbContextFactory();

        // Suma pe comanda (subquery corelat, translatat de provider) - restul
        // aritmeticii (aplicarea discountului) foloseste Math.Round, care nu
        // se translateaza in SQL, deci se face in memorie, la fel ca in
        // OrderService.MapeazaComandaAngajat.
        var comenzi = await context.Comenzi
            .Where(c => c.DataComanda >= start && c.DataComanda < sfarsitExclusiv)
            .Select(c => new
            {
                c.DataComanda,
                c.CostTransport,
                c.Discount,
                StareDenumire = c.Stare.Denumire,
                SubtotalMancare = c.ComandaDetalii.Sum(cd => cd.Cantitate * cd.PretUnitarLaComanda),
            })
            .ToListAsync(cancellationToken);

        var randuri = comenzi
            .Select(c => new
            {
                Data = c.DataComanda.Date,
                EsteAnulata = StariAnulate.Contains(c.StareDenumire),
                Total = c.SubtotalMancare - Math.Round(c.SubtotalMancare * c.Discount / 100m, 2) + c.CostTransport,
            })
            .ToList();

        var zile = randuri
            .GroupBy(r => r.Data)
            .Select(g => new VanzareZilnicaDto
            {
                Data = g.Key,
                NumarComenzi = g.Count(),
                NumarComenziAnulate = g.Count(r => r.EsteAnulata),
                SumaIncasata = g.Where(r => !r.EsteAnulata).Sum(r => r.Total),
            })
            .OrderBy(z => z.Data)
            .ToList();

        return new RaportVanzariDto
        {
            DataStart = start,
            DataEnd = dataEnd.Date,
            NumarComenzi = randuri.Count,
            NumarComenziAnulate = randuri.Count(r => r.EsteAnulata),
            SumaTotalaIncasata = randuri.Where(r => !r.EsteAnulata).Sum(r => r.Total),
            Zile = zile,
        };
    }

    public async Task<List<PreparatVandutDto>> RaportPreparateCelMaiVanduteAsync(
        DateTime dataStart,
        DateTime dataEnd,
        int top = 10,
        CancellationToken cancellationToken = default)
    {
        VerificaEsteAngajatSauArunca();

        if (top <= 0)
        {
            top = 10;
        }

        var (start, sfarsitExclusiv) = NormalizeazaInterval(dataStart, dataEnd);

        await using var context = _dbContextFactory();

        // Filtrarea pe stare ("anulata") se face in memorie, dupa incarcare -
        // StariAnulate e un HashSet cu comparator case-insensitive
        // (OrdinalIgnoreCase), a carui semantica nu s-ar pastra corect daca
        // ar fi translatat intr-un IN(...) SQL (colatia bazei de date nu e
        // garantat aceeasi). Acelasi motiv pentru care StariFinale din
        // OrderService nu e niciodata folosit direct intr-un Where LINQ.
        var linii = await context.ComandaDetalii
            .Where(cd => cd.Comanda.DataComanda >= start && cd.Comanda.DataComanda < sfarsitExclusiv)
            .Select(cd => new
            {
                cd.PreparatId,
                cd.MeniuId,
                StareComanda = cd.Comanda.Stare.Denumire,
                DenumirePreparat = cd.Preparat != null ? cd.Preparat.Denumire : null,
                DenumireMeniu = cd.Meniu != null ? cd.Meniu.Denumire : null,
                CategoriePreparat = cd.Preparat != null ? cd.Preparat.Categorie.Denumire : null,
                CategorieMeniu = cd.Meniu != null ? cd.Meniu.Categorie.Denumire : null,
                cd.Cantitate,
                Suma = cd.Cantitate * cd.PretUnitarLaComanda,
            })
            .ToListAsync(cancellationToken);

        return linii
            .Where(l => !StariAnulate.Contains(l.StareComanda))
            .GroupBy(l => (l.PreparatId, l.MeniuId))
            .Select(g =>
            {
                var primul = g.First();
                return new PreparatVandutDto
                {
                    Denumire = primul.DenumirePreparat ?? primul.DenumireMeniu ?? string.Empty,
                    Tip = primul.PreparatId.HasValue ? "Preparat" : "Meniu",
                    Categorie = primul.CategoriePreparat ?? primul.CategorieMeniu ?? string.Empty,
                    CantitateTotalaComandata = g.Sum(l => l.Cantitate),
                    SumaIncasata = g.Sum(l => l.Suma),
                };
            })
            .OrderByDescending(p => p.CantitateTotalaComandata)
            .Take(top)
            .ToList();
    }

    public async Task<List<VanzareCategorieDto>> RaportVanzariPeCategorieAsync(
        DateTime dataStart,
        DateTime dataEnd,
        CancellationToken cancellationToken = default)
    {
        VerificaEsteAngajatSauArunca();

        var (start, sfarsitExclusiv) = NormalizeazaInterval(dataStart, dataEnd);

        await using var context = _dbContextFactory();

        // Vezi comentariul din RaportPreparateCelMaiVanduteAsync - filtrarea
        // pe stare "anulata" se face in memorie, nu in Where LINQ.
        var linii = await context.ComandaDetalii
            .Where(cd => cd.Comanda.DataComanda >= start && cd.Comanda.DataComanda < sfarsitExclusiv)
            .Select(cd => new
            {
                StareComanda = cd.Comanda.Stare.Denumire,
                Categorie = cd.Preparat != null ? cd.Preparat.Categorie.Denumire : cd.Meniu!.Categorie.Denumire,
                cd.Cantitate,
                Suma = cd.Cantitate * cd.PretUnitarLaComanda,
            })
            .ToListAsync(cancellationToken);

        return linii
            .Where(l => !StariAnulate.Contains(l.StareComanda))
            .GroupBy(l => l.Categorie)
            .Select(g => new VanzareCategorieDto
            {
                Categorie = g.Key,
                CantitateTotala = g.Sum(l => l.Cantitate),
                SumaIncasata = g.Sum(l => l.Suma),
            })
            .OrderByDescending(v => v.SumaIncasata)
            .ToList();
    }

    public async Task<List<PreparatStocDto>> RaportStocCurentAsync(CancellationToken cancellationToken = default)
    {
        VerificaEsteAngajatSauArunca();

        await using var context = _dbContextFactory();

        return await context.Preparate
            .OrderBy(p => p.Categorie.Denumire)
            .ThenBy(p => p.Denumire)
            .Select(p => new PreparatStocDto
            {
                Id = p.Id,
                Denumire = p.Denumire,
                Categorie = p.Categorie.Denumire,
                CantitateTotalaRestaurant = p.CantitateTotalaRestaurant,
                UnitateMasura = p.UnitateMasura,
                Disponibil = p.Disponibil,
            })
            .ToListAsync(cancellationToken);
    }

    private void VerificaEsteAngajatSauArunca()
    {
        if (!_sessionService.EsteAutentificat || !_sessionService.EsteAngajat)
        {
            throw new UnauthorizedAccessException("Aceasta actiune este permisa doar angajatilor autentificati.");
        }
    }

    /// <summary>
    /// dataStart/dataEnd vin dintr-un selector de date (fara ora) - intervalul
    /// e tratat ca inclusiv la ambele capete, deci limita superioara e
    /// normalizata la inceputul zilei urmatoare pentru o comparatie
    /// "DataComanda &lt; sfarsitExclusiv" care include toate orele din
    /// dataEnd.
    /// </summary>
    private static (DateTime Start, DateTime SfarsitExclusiv) NormalizeazaInterval(DateTime dataStart, DateTime dataEnd)
    {
        return (dataStart.Date, dataEnd.Date.AddDays(1));
    }
}
