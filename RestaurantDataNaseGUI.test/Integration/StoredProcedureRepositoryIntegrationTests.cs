using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.test.TestSupport;
using Xunit;

namespace RestaurantDataNaseGUI.test.Integration;

/// <summary>
/// Teste de integrare directe pentru StoredProcedureRepository (proceduri
/// stocate T-SQL reale), impotriva unui SQL Server real - vezi
/// RestaurantDataNaseGUI.test/README.md.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class StoredProcedureRepositoryIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    public StoredProcedureRepositoryIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPreparateApropiateDeEpuizareAsync_ReturneazaPreparatulCuStocScazut()
    {
        var sufix = Guid.NewGuid().ToString("N")[..8];
        await using var context = _fixture.CreateContext();

        var categorie = new Categorie { Denumire = $"Categorie-Test-{sufix}" };
        var preparat = new Preparat
        {
            Denumire = $"Preparat-Test-{sufix}",
            Pret = 12m,
            CantitatePortie = 100m,
            UnitateMasura = "g",
            CantitateTotalaRestaurant = 3m, // sub PragStocEpuizare implicit (10, seed schema.sql)
            Disponibil = true,
        };
        categorie.Preparate.Add(preparat);
        context.Categorii.Add(categorie);
        await context.SaveChangesAsync();

        try
        {
            var repository = new StoredProcedureRepository(context);
            var rezultate = await repository.GetPreparateApropiateDeEpuizareAsync();

            var randPreparat = Assert.Single(rezultate, r => r.Id == preparat.Id);
            Assert.Equal(preparat.Denumire, randPreparat.Denumire);
            Assert.Equal(3m, randPreparat.CantitateTotalaRestaurant);
            Assert.True(randPreparat.Disponibil);
        }
        finally
        {
            await using var cleanupContext = _fixture.CreateContext();
            var p = await cleanupContext.Preparate.FindAsync(preparat.Id);
            if (p is not null) cleanupContext.Preparate.Remove(p);
            var c = await cleanupContext.Categorii.FindAsync(categorie.Id);
            if (c is not null) cleanupContext.Categorii.Remove(c);
            await cleanupContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task SetPreparatIndisponibilAsync_MarcheazaIndisponibil_FaraDeleteFizic()
    {
        var sufix = Guid.NewGuid().ToString("N")[..8];
        await using var context = _fixture.CreateContext();

        var categorie = new Categorie { Denumire = $"Categorie-Test-{sufix}" };
        var preparat = new Preparat
        {
            Denumire = $"Preparat-Test-{sufix}",
            Pret = 12m,
            CantitatePortie = 100m,
            UnitateMasura = "g",
            CantitateTotalaRestaurant = 50m,
            Disponibil = true,
        };
        categorie.Preparate.Add(preparat);
        context.Categorii.Add(categorie);
        await context.SaveChangesAsync();

        try
        {
            var repository = new StoredProcedureRepository(context);
            await repository.SetPreparatIndisponibilAsync(preparat.Id);

            await using var verifyContext = _fixture.CreateContext();
            var preparatActualizat = await verifyContext.Preparate.FirstOrDefaultAsync(p => p.Id == preparat.Id);

            Assert.NotNull(preparatActualizat); // nu a fost sters fizic
            Assert.False(preparatActualizat!.Disponibil);
        }
        finally
        {
            await using var cleanupContext = _fixture.CreateContext();
            var p = await cleanupContext.Preparate.FindAsync(preparat.Id);
            if (p is not null) cleanupContext.Preparate.Remove(p);
            var c = await cleanupContext.Categorii.FindAsync(categorie.Id);
            if (c is not null) cleanupContext.Categorii.Remove(c);
            await cleanupContext.SaveChangesAsync();
        }
    }
}
