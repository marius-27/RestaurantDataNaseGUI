using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>
/// Implementare IOrderService. Toate pragurile/procentele de discount si
/// transport se citesc din dbo.Configurare la fiecare calcul - niciodata
/// hardcodate in cod (vezi CitesteConfigurareAsync). Regulile de business
/// (vezi Services/README.md):
///
/// 1. Discount daca subtotalul > SumaMinimaComandaDiscount, SAU daca
///    clientul are mai mult de NumarComenziPentruDiscount comenzi in
///    ultimele IntervalTimpDiscount zile - in ambele cazuri se aplica
///    acelasi procent, ProcentDiscountFrecventa (schema nu are un procent
///    separat pentru "comanda mare"). Cele doua conditii NU se cumuleaza -
///    daca ambele sunt indeplinite, discountul se aplica o singura data.
/// 2. Cost transport CostTransport lei daca subtotalul e sub
///    PragTransportGratuit, altfel transport gratuit.
///
/// Actualizarea stocului (StoredProcedureRepository.UpdateCantitateTotalaLaComandaAsync)
/// NU se face aici - se face cand o comanda trece in starea "se pregateste",
/// lucru care apartine modulului de angajat (pas viitor).
/// </summary>
public class OrderService : IOrderService
{
    /// <summary>Starile "finale" ale unei comenzi - nu mai poate fi urmarita ca activa sau anulata.</summary>
    private static readonly HashSet<string> StariFinale = new(StringComparer.OrdinalIgnoreCase)
    {
        "livrata",
        "anulata",
    };

    private readonly ISessionService _sessionService;
    private readonly Func<RestaurantDbContext> _dbContextFactory;

    public OrderService(ISessionService? sessionService = null, Func<RestaurantDbContext>? dbContextFactory = null)
    {
        _sessionService = sessionService ?? SessionService.Instance;
        _dbContextFactory = dbContextFactory ?? (() => DatabaseConfig.CreateDbContext());
    }

    public async Task<CalculComandaDto> CalculeazaCostComandaAsync(
        List<ArticolCosDto> articole,
        int utilizatorId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory();
        return await CalculeazaCostAsync(context, articole, utilizatorId, cancellationToken);
    }

    public async Task<OrderResult> CreeazaComandaAsync(
        List<ArticolCosDto> articole,
        int utilizatorId,
        CancellationToken cancellationToken = default)
    {
        if (!_sessionService.EsteAutentificat || !_sessionService.EsteClient)
        {
            return OrderResult.Esec("Trebuie sa fii autentificat ca si client pentru a plasa o comanda.");
        }

        if (_sessionService.CurrentUser!.Id != utilizatorId)
        {
            return OrderResult.Esec("Nu poti plasa o comanda in numele altui utilizator.");
        }

        if (articole is null || articole.Count == 0)
        {
            return OrderResult.Esec("Cosul este gol.");
        }

        foreach (var articol in articole)
        {
            var areExactUnAtribut = articol.PreparatId.HasValue ^ articol.MeniuId.HasValue;
            if (!areExactUnAtribut)
            {
                return OrderResult.Esec("Fiecare articol din cos trebuie sa fie fie un preparat, fie un meniu.");
            }

            if (articol.Cantitate <= 0)
            {
                return OrderResult.Esec("Cantitatea fiecarui articol din cos trebuie sa fie pozitiva.");
            }
        }

        await using var context = _dbContextFactory();

        var mesajIndisponibilitate = await VerificaDisponibilitateaAsync(context, articole, cancellationToken);
        if (mesajIndisponibilitate is not null)
        {
            return OrderResult.Esec(mesajIndisponibilitate);
        }

        var calcul = await CalculeazaCostAsync(context, articole, utilizatorId, cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var repository = new StoredProcedureRepository(context);

            var (comandaId, codUnic) = await repository.CreateComandaAsync(
                utilizatorId,
                calcul.CostTransport,
                calcul.ProcentDiscount,
                cancellationToken);

            foreach (var articol in articole)
            {
                await repository.AdaugaDetaliuComandaAsync(
                    comandaId,
                    articol.PreparatId,
                    articol.MeniuId,
                    articol.Cantitate,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return OrderResult.Ok(comandaId, codUnic, calcul);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return OrderResult.Esec("Crearea comenzii a esuat. Incearca din nou.");
        }
    }

    public async Task<List<ComandaClientDto>> GetComenziClientAsync(
        int utilizatorId,
        CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory();
        var repository = new StoredProcedureRepository(context);

        var randuri = await repository.GetComenziClientCuDetaliiAsync(utilizatorId, cancellationToken);

        return randuri
            .GroupBy(r => r.ComandaId)
            .Select(grup =>
            {
                var primul = grup.First();
                var subtotalMancare = grup.Sum(r => r.SubTotal);
                var valoareDiscount = Math.Round(subtotalMancare * primul.Discount / 100m, 2);

                return new ComandaClientDto
                {
                    ComandaId = primul.ComandaId,
                    CodUnic = primul.CodUnic,
                    DataComanda = primul.DataComanda,
                    Stare = primul.Stare,
                    CostTransport = primul.CostTransport,
                    Discount = primul.Discount,
                    OraEstimataLivrare = primul.OraEstimataLivrare,
                    SubtotalMancare = subtotalMancare,
                    Total = subtotalMancare - valoareDiscount + primul.CostTransport,
                    EsteActiva = !StariFinale.Contains(primul.Stare),
                    Articole = grup
                        .Select(r => new ArticolComandaClientDto
                        {
                            Denumire = r.DenumireArticol,
                            Cantitate = r.Cantitate,
                            PretUnitar = r.PretUnitarLaComanda,
                        })
                        .ToList(),
                };
            })
            .OrderByDescending(c => c.DataComanda)
            .ToList();
    }

    public async Task<OrderResult> AnuleazaComandaAsync(
        int comandaId,
        int utilizatorId,
        CancellationToken cancellationToken = default)
    {
        if (!_sessionService.EsteAutentificat || !_sessionService.EsteClient)
        {
            return OrderResult.Esec("Trebuie sa fii autentificat ca si client pentru a anula o comanda.");
        }

        if (_sessionService.CurrentUser!.Id != utilizatorId)
        {
            return OrderResult.Esec("Nu poti anula o comanda a altui utilizator.");
        }

        await using var context = _dbContextFactory();

        var comanda = await context.Comenzi
            .Include(c => c.Stare)
            .FirstOrDefaultAsync(c => c.Id == comandaId, cancellationToken);

        if (comanda is null || comanda.UtilizatorId != utilizatorId)
        {
            return OrderResult.Esec("Comanda specificata nu exista sau nu iti apartine.");
        }

        if (StariFinale.Contains(comanda.Stare.Denumire))
        {
            return OrderResult.Esec($"Comanda este deja \"{comanda.Stare.Denumire}\" si nu mai poate fi anulata.");
        }

        var stareAnulata = await context.StariComanda
            .FirstOrDefaultAsync(s => s.Denumire == "anulata", cancellationToken);

        if (stareAnulata is null)
        {
            return OrderResult.Esec("Starea \"anulata\" nu exista in configurarea aplicatiei.");
        }

        comanda.StareId = stareAnulata.Id;
        await context.SaveChangesAsync(cancellationToken);

        return OrderResult.Ok(comanda.Id, comanda.CodUnic);
    }

    private static async Task<string?> VerificaDisponibilitateaAsync(
        RestaurantDbContext context,
        List<ArticolCosDto> articole,
        CancellationToken cancellationToken)
    {
        var preparatIds = articole
            .Where(a => a.PreparatId.HasValue)
            .Select(a => a.PreparatId!.Value)
            .Distinct()
            .ToList();

        var meniuIds = articole
            .Where(a => a.MeniuId.HasValue)
            .Select(a => a.MeniuId!.Value)
            .Distinct()
            .ToList();

        if (preparatIds.Count > 0)
        {
            var preparateDisponibile = await context.Preparate
                .CountAsync(p => preparatIds.Contains(p.Id) && p.Disponibil, cancellationToken);

            if (preparateDisponibile < preparatIds.Count)
            {
                return "Unul sau mai multe preparate din cos nu (mai) sunt disponibile.";
            }
        }

        if (meniuIds.Count > 0)
        {
            // Un meniu e disponibil doar daca TOATE preparatele lui componente sunt disponibile.
            var meniuriDisponibile = await context.Meniuri
                .CountAsync(
                    m => meniuIds.Contains(m.Id) && m.MeniuPreparate.All(mp => mp.Preparat.Disponibil),
                    cancellationToken);

            if (meniuriDisponibile < meniuIds.Count)
            {
                return "Unul sau mai multe meniuri din cos contin preparate indisponibile.";
            }
        }

        return null;
    }

    private static async Task<CalculComandaDto> CalculeazaCostAsync(
        RestaurantDbContext context,
        List<ArticolCosDto> articole,
        int utilizatorId,
        CancellationToken cancellationToken)
    {
        var subtotal = articole.Sum(a => a.PretUnitar * a.Cantitate);

        var configurare = await CitesteConfigurareAsync(context, cancellationToken);

        var esteComandaMare = subtotal > configurare.SumaMinimaComandaDiscount;

        var dataLimita = DateTime.UtcNow.AddDays(-configurare.IntervalTimpDiscount);
        var numarComenziRecente = await context.Comenzi
            .CountAsync(c => c.UtilizatorId == utilizatorId && c.DataComanda >= dataLimita, cancellationToken);
        var esteClientFrecvent = numarComenziRecente > configurare.NumarComenziPentruDiscount;

        var procentDiscount = 0m;
        string? motivDiscount = null;

        if (esteComandaMare || esteClientFrecvent)
        {
            // Acelasi procent pentru ambele conditii - schema are o singura
            // cheie de procent (ProcentDiscountFrecventa). Nu se cumuleaza:
            // discountul se aplica o singura data, indiferent cate conditii
            // sunt indeplinite simultan.
            procentDiscount = configurare.ProcentDiscountFrecventa;
            motivDiscount = (esteComandaMare, esteClientFrecvent) switch
            {
                (true, true) => $"Comanda peste {configurare.SumaMinimaComandaDiscount:0.##} lei si client frecvent",
                (true, false) => $"Comanda peste {configurare.SumaMinimaComandaDiscount:0.##} lei",
                _ => "Client frecvent",
            };
        }

        var valoareDiscount = Math.Round(subtotal * procentDiscount / 100m, 2);

        var costTransport = subtotal < configurare.PragTransportGratuit
            ? configurare.CostTransport
            : 0m;

        return new CalculComandaDto
        {
            SubtotalMancare = subtotal,
            ProcentDiscount = procentDiscount,
            ValoareDiscount = valoareDiscount,
            MotivDiscount = motivDiscount,
            CostTransport = costTransport,
            Total = subtotal - valoareDiscount + costTransport,
        };
    }

    private sealed record ConfigurareComanda(
        decimal SumaMinimaComandaDiscount,
        decimal ProcentDiscountFrecventa,
        int NumarComenziPentruDiscount,
        int IntervalTimpDiscount,
        decimal PragTransportGratuit,
        decimal CostTransport);

    private static async Task<ConfigurareComanda> CitesteConfigurareAsync(
        RestaurantDbContext context,
        CancellationToken cancellationToken)
    {
        var valori = await context.Configurari
            .ToDictionaryAsync(c => c.Cheie, c => c.Valoare, cancellationToken);

        return new ConfigurareComanda(
            CiteseDecimal(valori, "SumaMinimaComandaDiscount"),
            CiteseDecimal(valori, "ProcentDiscountFrecventa"),
            (int)CiteseDecimal(valori, "NumarComenziPentruDiscount"),
            (int)CiteseDecimal(valori, "IntervalTimpDiscount"),
            CiteseDecimal(valori, "PragTransportGratuit"),
            CiteseDecimal(valori, "CostTransport"));
    }

    private static decimal CiteseDecimal(Dictionary<string, string> valori, string cheie)
    {
        if (!valori.TryGetValue(cheie, out var text) ||
            !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var valoare))
        {
            throw new InvalidOperationException(
                $"Configurarea '{cheie}' lipseste din dbo.Configurare sau are o valoare invalida.");
        }

        return valoare;
    }
}
