using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;
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
/// Partea de angajat (vizualizare/schimbare stare comenzi) e in aceeasi
/// clasa, nu intr-un serviciu separat: SchimbaStareComandaAsync verifica
/// tranzitiile valide de stare (vezi TranzitiiValide) si, doar cand starea
/// noua e "se pregateste", apeleaza
/// StoredProcedureRepository.UpdateCantitateTotalaLaComandaAsync ca sa scada
/// stocul - cerinta explicita ("la fiecare comanda pusa in pregatire se
/// actualizeaza automat cantitatea totala din restaurant").
/// </summary>
public class OrderService : IOrderService
{
    /// <summary>Starile "finale" ale unei comenzi - nu mai poate fi urmarita ca activa sau anulata.</summary>
    private static readonly HashSet<string> StariFinale = new(StringComparer.OrdinalIgnoreCase)
    {
        "livrata",
        "anulata",
    };

    /// <summary>
    /// Tranzitiile valide de stare pentru o comanda: fluxul normal
    /// inregistrata -> se pregateste -> a plecat la client -> livrata, plus
    /// posibilitatea de a anula orice comanda activa. Orice alta tranzitie
    /// (ex. direct din "inregistrata" in "livrata") e respinsa.
    /// </summary>
    private static readonly Dictionary<string, string[]> TranzitiiValide = new(StringComparer.OrdinalIgnoreCase)
    {
        ["inregistrata"] = new[] { "se pregateste", "anulata" },
        ["se pregateste"] = new[] { "a plecat la client", "anulata" },
        ["a plecat la client"] = new[] { "livrata", "anulata" },
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

    public async Task<List<ComandaAngajatDto>> GetToateComenzileAsync(CancellationToken cancellationToken = default)
    {
        VerificaEsteAngajatSauArunca();

        await using var context = _dbContextFactory();

        var comenzi = await context.Comenzi
            .Include(c => c.Utilizator)
            .Include(c => c.Stare)
            .Include(c => c.ComandaDetalii).ThenInclude(cd => cd.Preparat)
            .Include(c => c.ComandaDetalii).ThenInclude(cd => cd.Meniu)
            .OrderByDescending(c => c.DataComanda)
            .ToListAsync(cancellationToken);

        return comenzi.Select(MapeazaComandaAngajat).ToList();
    }

    public async Task<List<ComandaAngajatDto>> GetComenziActiveAngajatAsync(CancellationToken cancellationToken = default)
    {
        var toateComenzile = await GetToateComenzileAsync(cancellationToken);
        return toateComenzile.Where(c => c.EsteActiva).ToList();
    }

    public IReadOnlyList<string> GetStariUrmatoarePosibile(string stareCurenta)
    {
        return TranzitiiValide.TryGetValue(stareCurenta ?? string.Empty, out var stariUrmatoare)
            ? stariUrmatoare
            : Array.Empty<string>();
    }

    public async Task<OrderResult> SchimbaStareComandaAsync(
        int comandaId,
        string stareNoua,
        CancellationToken cancellationToken = default)
    {
        if (!_sessionService.EsteAutentificat || !_sessionService.EsteAngajat)
        {
            return OrderResult.Esec("Doar angajatii autentificati pot schimba starea unei comenzi.");
        }

        stareNoua = (stareNoua ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(stareNoua))
        {
            return OrderResult.Esec("Starea noua este obligatorie.");
        }

        await using var context = _dbContextFactory();

        var comanda = await context.Comenzi
            .Include(c => c.Stare)
            .FirstOrDefaultAsync(c => c.Id == comandaId, cancellationToken);

        if (comanda is null)
        {
            return OrderResult.Esec("Comanda specificata nu exista.");
        }

        var stareCurenta = comanda.Stare.Denumire;

        if (StariFinale.Contains(stareCurenta))
        {
            return OrderResult.Esec($"Comanda este deja \"{stareCurenta}\" si nu isi mai poate schimba starea.");
        }

        var stariUrmatoarePosibile = GetStariUrmatoarePosibile(stareCurenta);
        if (!stariUrmatoarePosibile.Any(s => string.Equals(s, stareNoua, StringComparison.OrdinalIgnoreCase)))
        {
            return OrderResult.Esec($"Nu se poate trece direct din starea \"{stareCurenta}\" in \"{stareNoua}\".");
        }

        var stareNouaEntitate = await context.StariComanda
            .FirstOrDefaultAsync(s => s.Denumire == stareNoua, cancellationToken);

        if (stareNouaEntitate is null)
        {
            return OrderResult.Esec($"Starea \"{stareNoua}\" nu exista in configurarea aplicatiei.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            comanda.StareId = stareNouaEntitate.Id;
            await context.SaveChangesAsync(cancellationToken);

            if (string.Equals(stareNoua, "se pregateste", StringComparison.OrdinalIgnoreCase))
            {
                // Cerinta explicita: la fiecare comanda pusa "in pregatire" se
                // actualizeaza automat stocul. sp_UpdateCantitateTotalaLaComanda
                // face propriul ROLLBACK + RAISERROR daca stocul e insuficient,
                // ceea ce arunca aici o SqlException - prinsa mai jos, ea anuleaza
                // si schimbarea de stare, ca cele doua sa ramana atomice.
                var repository = new StoredProcedureRepository(context);
                await repository.UpdateCantitateTotalaLaComandaAsync(comandaId, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return OrderResult.Ok(comanda.Id, comanda.CodUnic);
        }
        catch (Exception)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch
            {
                // sp_UpdateCantitateTotalaLaComanda executa propriul ROLLBACK
                // TRANSACTION cand stocul e insuficient, care poate incheia
                // tranzactia la nivel de server inainte sa ajungem aici - un
                // rollback "dublu" e sigur de ignorat, rezultatul dorit (nimic
                // nu se salveaza) e deja garantat de ROLLBACK-ul din procedura.
            }

            return OrderResult.Esec(
                "Schimbarea starii a esuat, probabil din cauza stocului insuficient pentru unul dintre preparate. Comanda a ramas in starea anterioara.");
        }
    }

    private static ComandaAngajatDto MapeazaComandaAngajat(Comanda comanda)
    {
        var subtotalMancare = comanda.ComandaDetalii.Sum(cd => cd.Cantitate * cd.PretUnitarLaComanda);
        var valoareDiscount = Math.Round(subtotalMancare * comanda.Discount / 100m, 2);

        return new ComandaAngajatDto
        {
            ComandaId = comanda.Id,
            CodUnic = comanda.CodUnic,
            DataComanda = comanda.DataComanda,
            Stare = comanda.Stare.Denumire,
            CostTransport = comanda.CostTransport,
            Discount = comanda.Discount,
            OraEstimataLivrare = comanda.OraEstimataLivrare,
            SubtotalMancare = subtotalMancare,
            Total = subtotalMancare - valoareDiscount + comanda.CostTransport,
            EsteActiva = !StariFinale.Contains(comanda.Stare.Denumire),
            Articole = comanda.ComandaDetalii
                .Select(cd => new ArticolComandaClientDto
                {
                    Denumire = cd.Preparat?.Denumire ?? cd.Meniu?.Denumire ?? string.Empty,
                    Cantitate = cd.Cantitate,
                    PretUnitar = cd.PretUnitarLaComanda,
                })
                .ToList(),
            NumeClient = comanda.Utilizator.Nume,
            PrenumeClient = comanda.Utilizator.Prenume,
            TelefonClient = comanda.Utilizator.Telefon,
            AdresaLivrareClient = comanda.Utilizator.AdresaLivrare,
        };
    }

    private void VerificaEsteAngajatSauArunca()
    {
        if (!_sessionService.EsteAutentificat || !_sessionService.EsteAngajat)
        {
            throw new UnauthorizedAccessException("Aceasta actiune este permisa doar angajatilor autentificati.");
        }
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
