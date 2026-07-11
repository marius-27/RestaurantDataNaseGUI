using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// Implementare IAdminService. Fiecare Create/Update/Delete verifica intai
// ISessionService.EsteAngajat (via VerificaEsteAngajat) - niciun apel nu ajunge la
// baza de date fara un angajat autentificat. Interogari LINQ peste EF Core, nu SQL brut.
public class AdminService : IAdminService
{
    private const int SqlErrorUniqueConstraint = 2627;
    private const int SqlErrorUniqueIndex = 2601;

    private readonly ISessionService _sessionService;
    private readonly Func<RestaurantDbContext> _dbContextFactory;

    public AdminService(ISessionService? sessionService = null, Func<RestaurantDbContext>? dbContextFactory = null)
    {
        _sessionService = sessionService ?? SessionService.Instance;
        _dbContextFactory = dbContextFactory ?? (() => DatabaseConfig.CreateDbContext());
    }

    // ----------------------------------------------------------------
    // Categorie
    // ----------------------------------------------------------------

    public async Task<List<Categorie>> GetCategoriiAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory();
        return await context.Categorii
            .OrderBy(c => c.Denumire)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminResult> CreeazaCategorieAsync(CategorieFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        var denumire = (form.Denumire ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(denumire))
        {
            return AdminResult.Esec("Denumirea categoriei este obligatorie.");
        }

        await using var context = _dbContextFactory();

        if (await context.Categorii.AnyAsync(c => c.Denumire == denumire, cancellationToken))
        {
            return AdminResult.Esec("Exista deja o categorie cu aceasta denumire.");
        }

        context.Categorii.Add(new Categorie { Denumire = denumire });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsteIncalcareUnicitate(ex))
        {
            return AdminResult.Esec("Exista deja o categorie cu aceasta denumire.");
        }

        return AdminResult.Ok();
    }

    public async Task<AdminResult> ActualizeazaCategorieAsync(CategorieFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        var denumire = (form.Denumire ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(denumire))
        {
            return AdminResult.Esec("Denumirea categoriei este obligatorie.");
        }

        await using var context = _dbContextFactory();

        var categorie = await context.Categorii.FirstOrDefaultAsync(c => c.Id == form.Id, cancellationToken);
        if (categorie is null)
        {
            return AdminResult.Esec("Categoria specificata nu exista.");
        }

        if (await context.Categorii.AnyAsync(c => c.Id != form.Id && c.Denumire == denumire, cancellationToken))
        {
            return AdminResult.Esec("Exista deja o categorie cu aceasta denumire.");
        }

        categorie.Denumire = denumire;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsteIncalcareUnicitate(ex))
        {
            return AdminResult.Esec("Exista deja o categorie cu aceasta denumire.");
        }

        return AdminResult.Ok();
    }

    public async Task<AdminResult> StergeCategorieAsync(int categorieId, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        await using var context = _dbContextFactory();

        var categorie = await context.Categorii.FirstOrDefaultAsync(c => c.Id == categorieId, cancellationToken);
        if (categorie is null)
        {
            return AdminResult.Esec("Categoria specificata nu exista.");
        }

        var areElemente = await context.Preparate.AnyAsync(p => p.CategorieId == categorieId, cancellationToken)
            || await context.Meniuri.AnyAsync(m => m.CategorieId == categorieId, cancellationToken);

        if (areElemente)
        {
            return AdminResult.Esec("Categoria are preparate sau meniuri asociate si nu poate fi stearsa.");
        }

        context.Categorii.Remove(categorie);
        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    // ----------------------------------------------------------------
    // Alergen
    // ----------------------------------------------------------------

    public async Task<List<Alergen>> GetAlergeniAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory();
        return await context.Alergeni
            .OrderBy(a => a.Denumire)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminResult> CreeazaAlergenAsync(AlergenFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        var denumire = (form.Denumire ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(denumire))
        {
            return AdminResult.Esec("Denumirea alergenului este obligatorie.");
        }

        await using var context = _dbContextFactory();

        if (await context.Alergeni.AnyAsync(a => a.Denumire == denumire, cancellationToken))
        {
            return AdminResult.Esec("Exista deja un alergen cu aceasta denumire.");
        }

        context.Alergeni.Add(new Alergen { Denumire = denumire });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsteIncalcareUnicitate(ex))
        {
            return AdminResult.Esec("Exista deja un alergen cu aceasta denumire.");
        }

        return AdminResult.Ok();
    }

    public async Task<AdminResult> ActualizeazaAlergenAsync(AlergenFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        var denumire = (form.Denumire ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(denumire))
        {
            return AdminResult.Esec("Denumirea alergenului este obligatorie.");
        }

        await using var context = _dbContextFactory();

        var alergen = await context.Alergeni.FirstOrDefaultAsync(a => a.Id == form.Id, cancellationToken);
        if (alergen is null)
        {
            return AdminResult.Esec("Alergenul specificat nu exista.");
        }

        if (await context.Alergeni.AnyAsync(a => a.Id != form.Id && a.Denumire == denumire, cancellationToken))
        {
            return AdminResult.Esec("Exista deja un alergen cu aceasta denumire.");
        }

        alergen.Denumire = denumire;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsteIncalcareUnicitate(ex))
        {
            return AdminResult.Esec("Exista deja un alergen cu aceasta denumire.");
        }

        return AdminResult.Ok();
    }

    public async Task<AdminResult> StergeAlergenAsync(int alergenId, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        await using var context = _dbContextFactory();

        var alergen = await context.Alergeni.FirstOrDefaultAsync(a => a.Id == alergenId, cancellationToken);
        if (alergen is null)
        {
            return AdminResult.Esec("Alergenul specificat nu exista.");
        }

        if (await context.PreparatAlergeni.AnyAsync(pa => pa.AlergenId == alergenId, cancellationToken))
        {
            return AdminResult.Esec("Alergenul este asociat unor preparate si nu poate fi sters.");
        }

        context.Alergeni.Remove(alergen);
        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    // ----------------------------------------------------------------
    // Preparat
    // ----------------------------------------------------------------

    public async Task<List<Preparat>> GetPreparateAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory();
        return await context.Preparate
            .Include(p => p.Categorie)
            .Include(p => p.PreparatAlergeni).ThenInclude(pa => pa.Alergen)
            .Include(p => p.Imagini)
            .OrderBy(p => p.Denumire)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminResult> CreeazaPreparatAsync(PreparatFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        if (ValideazaPreparat(form) is { } eroareValidare)
        {
            return AdminResult.Esec(eroareValidare);
        }

        await using var context = _dbContextFactory();

        if (!await context.Categorii.AnyAsync(c => c.Id == form.CategorieId, cancellationToken))
        {
            return AdminResult.Esec("Categoria selectata nu exista.");
        }

        var alergenIds = form.AlergenIds.Distinct().ToList();
        if (await context.Alergeni.CountAsync(a => alergenIds.Contains(a.Id), cancellationToken) != alergenIds.Count)
        {
            return AdminResult.Esec("Unul sau mai multi alergeni selectati nu exista.");
        }

        var preparat = new Preparat
        {
            Denumire = form.Denumire.Trim(),
            Pret = form.Pret,
            CantitatePortie = form.CantitatePortie,
            UnitateMasura = form.UnitateMasura.Trim(),
            CantitateTotalaRestaurant = form.CantitateTotalaRestaurant,
            CategorieId = form.CategorieId,
            Disponibil = form.Disponibil,
        };

        foreach (var alergenId in alergenIds)
        {
            preparat.PreparatAlergeni.Add(new PreparatAlergen { AlergenId = alergenId });
        }

        foreach (var cale in form.ImaginiPaths.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            preparat.Imagini.Add(new PreparatImagine { CalePoza = cale.Trim() });
        }

        context.Preparate.Add(preparat);
        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    public async Task<AdminResult> ActualizeazaPreparatAsync(PreparatFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        if (ValideazaPreparat(form) is { } eroareValidare)
        {
            return AdminResult.Esec(eroareValidare);
        }

        await using var context = _dbContextFactory();

        var preparat = await context.Preparate
            .Include(p => p.PreparatAlergeni)
            .Include(p => p.Imagini)
            .FirstOrDefaultAsync(p => p.Id == form.Id, cancellationToken);

        if (preparat is null)
        {
            return AdminResult.Esec("Preparatul specificat nu exista.");
        }

        if (!await context.Categorii.AnyAsync(c => c.Id == form.CategorieId, cancellationToken))
        {
            return AdminResult.Esec("Categoria selectata nu exista.");
        }

        var alergenIds = form.AlergenIds.Distinct().ToList();
        if (await context.Alergeni.CountAsync(a => alergenIds.Contains(a.Id), cancellationToken) != alergenIds.Count)
        {
            return AdminResult.Esec("Unul sau mai multi alergeni selectati nu exista.");
        }

        preparat.Denumire = form.Denumire.Trim();
        preparat.Pret = form.Pret;
        preparat.CantitatePortie = form.CantitatePortie;
        preparat.UnitateMasura = form.UnitateMasura.Trim();
        preparat.CantitateTotalaRestaurant = form.CantitateTotalaRestaurant;
        preparat.CategorieId = form.CategorieId;
        preparat.Disponibil = form.Disponibil;

        // Inlocuieste integral asocierile existente - simplu si corect pentru
        // un formular care trimite intreaga lista noua, in loc sa faca un
        // diff (adaugate/sterse) fata de starea anterioara.
        context.PreparatAlergeni.RemoveRange(preparat.PreparatAlergeni.ToList());
        foreach (var alergenId in alergenIds)
        {
            context.PreparatAlergeni.Add(new PreparatAlergen { PreparatId = preparat.Id, AlergenId = alergenId });
        }

        context.PreparatImagini.RemoveRange(preparat.Imagini.ToList());
        foreach (var cale in form.ImaginiPaths.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            context.PreparatImagini.Add(new PreparatImagine { PreparatId = preparat.Id, CalePoza = cale.Trim() });
        }

        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    public async Task<AdminResult> StergePreparatAsync(int preparatId, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        await using var context = _dbContextFactory();

        var preparat = await context.Preparate.FirstOrDefaultAsync(p => p.Id == preparatId, cancellationToken);
        if (preparat is null)
        {
            return AdminResult.Esec("Preparatul specificat nu exista.");
        }

        if (await context.ComandaDetalii.AnyAsync(cd => cd.PreparatId == preparatId, cancellationToken))
        {
            // Soft-delete: preparatul a fost deja folosit intr-o comanda, nu
            // se poate sterge fizic (vezi conventia din database/README.md).
            var repository = new StoredProcedureRepository(context);
            await repository.SetPreparatIndisponibilAsync(preparatId, cancellationToken);
            return AdminResult.Ok();
        }

        if (await context.MeniuPreparate.AnyAsync(mp => mp.PreparatId == preparatId, cancellationToken))
        {
            return AdminResult.Esec("Preparatul face parte din unul sau mai multe meniuri si nu poate fi sters. Scoate-l mai intai din meniuri.");
        }

        context.Preparate.Remove(preparat);
        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    // ----------------------------------------------------------------
    // Meniu
    // ----------------------------------------------------------------

    public async Task<List<Meniu>> GetMeniuriAsync(CancellationToken cancellationToken = default)
    {
        await using var context = _dbContextFactory();
        return await context.Meniuri
            .Include(m => m.Categorie)
            .Include(m => m.MeniuPreparate).ThenInclude(mp => mp.Preparat)
            .OrderBy(m => m.Denumire)
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminResult> CreeazaMeniuAsync(MeniuFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        if (ValideazaMeniu(form) is { } eroareValidare)
        {
            return AdminResult.Esec(eroareValidare);
        }

        await using var context = _dbContextFactory();

        if (!await context.Categorii.AnyAsync(c => c.Id == form.CategorieId, cancellationToken))
        {
            return AdminResult.Esec("Categoria selectata nu exista.");
        }

        var preparatIds = form.Preparate.Select(p => p.PreparatId).Distinct().ToList();
        if (await context.Preparate.CountAsync(p => preparatIds.Contains(p.Id), cancellationToken) != preparatIds.Count)
        {
            return AdminResult.Esec("Unul sau mai multe preparate selectate nu exista.");
        }

        var meniu = new Meniu
        {
            Denumire = form.Denumire.Trim(),
            CategorieId = form.CategorieId,
        };

        foreach (var componenta in form.Preparate)
        {
            meniu.MeniuPreparate.Add(new MeniuPreparat
            {
                PreparatId = componenta.PreparatId,
                CantitateInMeniu = componenta.CantitateInMeniu,
            });
        }

        context.Meniuri.Add(meniu);
        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    public async Task<AdminResult> ActualizeazaMeniuAsync(MeniuFormDto form, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        if (ValideazaMeniu(form) is { } eroareValidare)
        {
            return AdminResult.Esec(eroareValidare);
        }

        await using var context = _dbContextFactory();

        var meniu = await context.Meniuri
            .Include(m => m.MeniuPreparate)
            .FirstOrDefaultAsync(m => m.Id == form.Id, cancellationToken);

        if (meniu is null)
        {
            return AdminResult.Esec("Meniul specificat nu exista.");
        }

        if (!await context.Categorii.AnyAsync(c => c.Id == form.CategorieId, cancellationToken))
        {
            return AdminResult.Esec("Categoria selectata nu exista.");
        }

        var preparatIds = form.Preparate.Select(p => p.PreparatId).Distinct().ToList();
        if (await context.Preparate.CountAsync(p => preparatIds.Contains(p.Id), cancellationToken) != preparatIds.Count)
        {
            return AdminResult.Esec("Unul sau mai multe preparate selectate nu exista.");
        }

        meniu.Denumire = form.Denumire.Trim();
        meniu.CategorieId = form.CategorieId;

        // Inlocuieste integral componentele existente, la fel ca la Preparat.
        context.MeniuPreparate.RemoveRange(meniu.MeniuPreparate.ToList());
        foreach (var componenta in form.Preparate)
        {
            context.MeniuPreparate.Add(new MeniuPreparat
            {
                MeniuId = meniu.Id,
                PreparatId = componenta.PreparatId,
                CantitateInMeniu = componenta.CantitateInMeniu,
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    public async Task<AdminResult> StergeMeniuAsync(int meniuId, CancellationToken cancellationToken = default)
    {
        if (VerificaEsteAngajat() is { } refuz) return refuz;

        await using var context = _dbContextFactory();

        var meniu = await context.Meniuri.FirstOrDefaultAsync(m => m.Id == meniuId, cancellationToken);
        if (meniu is null)
        {
            return AdminResult.Esec("Meniul specificat nu exista.");
        }

        // Nu exista soft-delete pentru Meniu in schema - o comanda anterioara
        // ar bloca oricum DELETE-ul fizic (FK_ComandaDetaliu_Meniu e Restrict),
        // deci verificam explicit si dam un mesaj clar in loc sa lasam sa
        // esueze cu o eroare SQL bruta.
        if (await context.ComandaDetalii.AnyAsync(cd => cd.MeniuId == meniuId, cancellationToken))
        {
            return AdminResult.Esec("Meniul a fost folosit in comenzi si nu poate fi sters.");
        }

        context.Meniuri.Remove(meniu);
        await context.SaveChangesAsync(cancellationToken);

        return AdminResult.Ok();
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private AdminResult? VerificaEsteAngajat()
    {
        if (!_sessionService.EsteAutentificat || !_sessionService.EsteAngajat)
        {
            return AdminResult.Esec("Aceasta actiune este permisa doar angajatilor autentificati.");
        }

        return null;
    }

    private static string? ValideazaPreparat(PreparatFormDto form)
    {
        if (string.IsNullOrWhiteSpace(form.Denumire))
        {
            return "Denumirea preparatului este obligatorie.";
        }

        if (form.Pret <= 0)
        {
            return "Pretul trebuie sa fie pozitiv.";
        }

        if (form.CantitatePortie <= 0)
        {
            return "Cantitatea per portie trebuie sa fie pozitiva.";
        }

        if (string.IsNullOrWhiteSpace(form.UnitateMasura))
        {
            return "Unitatea de masura este obligatorie.";
        }

        if (form.CantitateTotalaRestaurant < 0)
        {
            return "Cantitatea totala din stoc nu poate fi negativa.";
        }

        return null;
    }

    private static string? ValideazaMeniu(MeniuFormDto form)
    {
        if (string.IsNullOrWhiteSpace(form.Denumire))
        {
            return "Denumirea meniului este obligatorie.";
        }

        if (form.Preparate is null || form.Preparate.Count == 0)
        {
            return "Meniul trebuie sa contina cel putin un preparat.";
        }

        if (form.Preparate.Any(p => p.CantitateInMeniu <= 0))
        {
            return "Cantitatea fiecarui preparat din meniu trebuie sa fie pozitiva.";
        }

        return null;
    }

    private static bool EsteIncalcareUnicitate(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == SqlErrorUniqueConstraint || sqlEx.Number == SqlErrorUniqueIndex);
    }
}
