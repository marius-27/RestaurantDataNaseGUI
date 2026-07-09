using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>
/// Implementare IMenuService. Preparatele individuale se citesc direct prin
/// EF Core LINQ (parametrizat automat). Meniurile compuse se citesc prin
/// StoredProcedureRepository.GetMeniuRestaurantCuAlergeniAsync (procedura
/// stocata dbo.sp_GetMeniuRestaurantCuAlergeni), care da pretul calculat
/// dinamic (dbo.fn_CalculeazaPretMeniu) si alergenii agregati din toate
/// preparatele componente; procedura nu returneaza si disponibilitatea, asa
/// ca aceasta se calculeaza separat printr-o interogare LINQ.
/// </summary>
public class MenuService : IMenuService
{
    private readonly Func<RestaurantDbContext> _dbContextFactory;

    public MenuService(Func<RestaurantDbContext>? dbContextFactory = null)
    {
        _dbContextFactory = dbContextFactory ?? (() => DatabaseConfig.CreateDbContext());
    }

    public async Task<List<CategorieMeniuDto>> GetMeniuRestaurantAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory();
        var repository = new StoredProcedureRepository(context);

        // Secvential, nu in paralel: acelasi RestaurantDbContext nu suporta
        // mai multe operatii async concurente.
        var preparate = await GetPreparateAsync(context, cancellationToken);
        var meniuri = await GetMeniuriAsync(context, repository, cancellationToken);

        return preparate
            .Concat(meniuri)
            .GroupBy(item => item.Categorie, StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(grup => grup.Key, StringComparer.CurrentCultureIgnoreCase)
            .Select(grup => new CategorieMeniuDto
            {
                Denumire = grup.Key,
                Itemi = grup.OrderBy(item => item.Denumire, StringComparer.CurrentCultureIgnoreCase).ToList(),
            })
            .ToList();
    }

    private static async Task<List<MeniuAfisareDto>> GetPreparateAsync(
        RestaurantDbContext context,
        CancellationToken cancellationToken)
    {
        var preparate = await context.Preparate
            .Include(p => p.Categorie)
            .Include(p => p.PreparatAlergeni).ThenInclude(pa => pa.Alergen)
            .Include(p => p.Imagini)
            .ToListAsync(cancellationToken);

        return preparate
            .Select(p => new MeniuAfisareDto
            {
                Id = p.Id,
                Denumire = p.Denumire,
                Categorie = p.Categorie.Denumire,
                Pret = p.Pret,
                CantitatePortie = p.CantitatePortie,
                UnitateMasura = p.UnitateMasura,
                ListaAlergeni = p.PreparatAlergeni
                    .Select(pa => pa.Alergen.Denumire)
                    .OrderBy(denumire => denumire, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                ListaImaginiPath = p.Imagini.Select(i => i.CalePoza).ToList(),
                EsteMeniuCompus = false,
                EsteIndisponibil = !p.Disponibil,
            })
            .ToList();
    }

    private static async Task<List<MeniuAfisareDto>> GetMeniuriAsync(
        RestaurantDbContext context,
        StoredProcedureRepository repository,
        CancellationToken cancellationToken)
    {
        var meniuriSp = await repository.GetMeniuRestaurantCuAlergeniAsync(cancellationToken);
        if (meniuriSp.Count == 0)
        {
            return new List<MeniuAfisareDto>();
        }

        var meniuIds = meniuriSp.Select(m => m.MeniuId).ToList();

        // dbo.sp_GetMeniuRestaurantCuAlergeni nu expune disponibilitatea, deci
        // se determina separat: un meniu e indisponibil daca cel putin un
        // preparat component e indisponibil.
        var indisponibilitate = await context.Meniuri
            .Where(m => meniuIds.Contains(m.Id))
            .Select(m => new
            {
                m.Id,
                EsteIndisponibil = m.MeniuPreparate.Any(mp => !mp.Preparat.Disponibil),
            })
            .ToDictionaryAsync(x => x.Id, x => x.EsteIndisponibil, cancellationToken);

        return meniuriSp
            .Select(m => new MeniuAfisareDto
            {
                Id = m.MeniuId,
                Denumire = m.Meniu,
                Categorie = m.Categorie,
                Pret = m.PretCalculat,
                CantitatePortie = null,
                UnitateMasura = null,
                ListaAlergeni = string.IsNullOrWhiteSpace(m.Alergeni)
                    ? Array.Empty<string>()
                    : m.Alergeni.Split(", ", StringSplitOptions.RemoveEmptyEntries),
                ListaImaginiPath = Array.Empty<string>(),
                EsteMeniuCompus = true,
                EsteIndisponibil = indisponibilitate.GetValueOrDefault(m.MeniuId),
            })
            .ToList();
    }
}
