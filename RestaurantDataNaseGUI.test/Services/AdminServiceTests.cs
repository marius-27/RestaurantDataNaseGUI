using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;
using RestaurantDataNaseGUI.test.TestSupport;
using Xunit;

namespace RestaurantDataNaseGUI.test.Services;

// Teste unitare pentru AdminService pe SQLite in-memory: reguli de blocare a stergerii si validari de formular, verificate in C#.
// NOTA: scenariul "stergere preparat folosit intr-o comanda -> soft-delete" foloseste o procedura stocata T-SQL (nu exista pe
// SQLite) si e testat separat, in Integration/AdminServiceIntegrationTests.cs. Aici testam doar blocarea stergerii unui
// Preparat care face parte dintr-un meniu (verificata integral prin LINQ).
public sealed class AdminServiceTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _dbFactory = new();
    private readonly Mock<ISessionService> _sessionServiceMock = new();

    public AdminServiceTests()
    {
        _sessionServiceMock.Setup(s => s.EsteAutentificat).Returns(true);
        _sessionServiceMock.Setup(s => s.EsteAngajat).Returns(true);
    }

    public void Dispose() => _dbFactory.Dispose();

    private AdminService CreateService() => new(_sessionServiceMock.Object, _dbFactory.CreateContext);

    private async Task<int> SeedCategorieAsync(string denumire = "Fel principal")
    {
        await using var context = _dbFactory.CreateContext();
        var categorie = new Categorie { Denumire = denumire };
        context.Categorii.Add(categorie);
        await context.SaveChangesAsync();
        return categorie.Id;
    }

    private async Task<int> SeedPreparatAsync(int categorieId, string denumire = "Ciorba de burta")
    {
        await using var context = _dbFactory.CreateContext();
        var preparat = new Preparat
        {
            Denumire = denumire,
            Pret = 20m,
            CantitatePortie = 350m,
            UnitateMasura = "ml",
            CantitateTotalaRestaurant = 5000m,
            CategorieId = categorieId,
            Disponibil = true,
        };
        context.Preparate.Add(preparat);
        await context.SaveChangesAsync();
        return preparat.Id;
    }

    [Fact]
    public async Task StergeCategorieAsync_CuPreparateAsociate_EsteBlocata()
    {
        var categorieId = await SeedCategorieAsync();
        await SeedPreparatAsync(categorieId);

        var service = CreateService();
        var rezultat = await service.StergeCategorieAsync(categorieId);

        Assert.False(rezultat.Succes);
        Assert.Contains("preparate sau meniuri asociate", rezultat.MesajEroare);

        await using var verifyContext = _dbFactory.CreateContext();
        Assert.True(await verifyContext.Categorii.AnyAsync(c => c.Id == categorieId));
    }

    [Fact]
    public async Task StergePreparatAsync_ParteDintrUnMeniu_EsteBlocata()
    {
        var categorieId = await SeedCategorieAsync();
        var preparatId = await SeedPreparatAsync(categorieId);

        int meniuId;
        await using (var context = _dbFactory.CreateContext())
        {
            var meniu = new Meniu { Denumire = "Meniu zilei", CategorieId = categorieId };
            meniu.MeniuPreparate.Add(new MeniuPreparat { PreparatId = preparatId, CantitateInMeniu = 1 });
            context.Meniuri.Add(meniu);
            await context.SaveChangesAsync();
            meniuId = meniu.Id;
        }

        var service = CreateService();
        var rezultat = await service.StergePreparatAsync(preparatId);

        Assert.False(rezultat.Succes);
        Assert.Contains("face parte din unul sau mai multe meniuri", rezultat.MesajEroare);

        await using var verifyContext = _dbFactory.CreateContext();
        Assert.True(await verifyContext.Preparate.AnyAsync(p => p.Id == preparatId));
        Assert.True(await verifyContext.Meniuri.AnyAsync(m => m.Id == meniuId));
    }

    [Fact]
    public async Task CreeazaPreparatAsync_PretNegativ_EsteRespins()
    {
        var categorieId = await SeedCategorieAsync();
        var service = CreateService();

        var form = new PreparatFormDto
        {
            Denumire = "Preparat cu pret invalid",
            Pret = -10m,
            CantitatePortie = 200m,
            UnitateMasura = "g",
            CantitateTotalaRestaurant = 10m,
            CategorieId = categorieId,
        };

        var rezultat = await service.CreeazaPreparatAsync(form);

        Assert.False(rezultat.Succes);
        Assert.Equal("Pretul trebuie sa fie pozitiv.", rezultat.MesajEroare);

        await using var verifyContext = _dbFactory.CreateContext();
        Assert.False(await verifyContext.Preparate.AnyAsync(p => p.Denumire == form.Denumire));
    }

    [Fact]
    public async Task CreeazaPreparatAsync_CantitatePortieNegativa_EsteRespinsa()
    {
        var categorieId = await SeedCategorieAsync();
        var service = CreateService();

        var form = new PreparatFormDto
        {
            Denumire = "Preparat cu cantitate invalida",
            Pret = 15m,
            CantitatePortie = -1m,
            UnitateMasura = "g",
            CantitateTotalaRestaurant = 10m,
            CategorieId = categorieId,
        };

        var rezultat = await service.CreeazaPreparatAsync(form);

        Assert.False(rezultat.Succes);
        Assert.Equal("Cantitatea per portie trebuie sa fie pozitiva.", rezultat.MesajEroare);

        await using var verifyContext = _dbFactory.CreateContext();
        Assert.False(await verifyContext.Preparate.AnyAsync(p => p.Denumire == form.Denumire));
    }
}
