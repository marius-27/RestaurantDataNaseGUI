using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;
using RestaurantDataNaseGUI.test.TestSupport;
using Xunit;

namespace RestaurantDataNaseGUI.test.Services;

// Teste unitare pentru OrderService pe SQLite in-memory. Acopera doar caile care nu ating StoredProcedureRepository
// (proceduri T-SQL inexistente pe SQLite): CreeazaComandaAsync (sp_CreateComanda) si SchimbaStareComandaAsync catre
// "se pregateste" (sp_UpdateCantitateTotalaLaComanda) sunt testate cu SQL Server real, in Integration/OrderServiceIntegrationTests.cs.
public sealed class OrderServiceTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _dbFactory = new();
    private readonly Mock<ISessionService> _sessionServiceMock = new();

    public void Dispose() => _dbFactory.Dispose();

    private OrderService CreateService() => new(_sessionServiceMock.Object, _dbFactory.CreateContext);

    private async Task<int> SeedClientAsync(string email = "client@test.ro")
    {
        await using var context = _dbFactory.CreateContext();
        var utilizator = new Utilizator
        {
            Nume = "Popescu",
            Prenume = "Ion",
            Email = email,
            Telefon = "0712345678",
            ParolaHash = "hash-nu-conteaza-in-acest-test",
            TipUtilizator = "Client",
        };
        context.Utilizatori.Add(utilizator);
        await context.SaveChangesAsync();
        return utilizator.Id;
    }

    private async Task SeedComenziRecenteAsync(int utilizatorId, int numar)
    {
        await using var context = _dbFactory.CreateContext();
        var stareId = await context.StariComanda.Where(s => s.Denumire == "inregistrata").Select(s => s.Id).FirstAsync();

        for (var i = 0; i < numar; i++)
        {
            context.Comenzi.Add(new Comanda
            {
                CodUnic = $"CMD-TEST-{utilizatorId}-{i}",
                UtilizatorId = utilizatorId,
                DataComanda = DateTime.UtcNow.AddDays(-1),
                StareId = stareId,
                CostTransport = 0,
                Discount = 0,
            });
        }

        await context.SaveChangesAsync();
    }

    private async Task<int> GetStareIdAsync(string denumire)
    {
        await using var context = _dbFactory.CreateContext();
        return await context.StariComanda.Where(s => s.Denumire == denumire).Select(s => s.Id).FirstAsync();
    }

    // ------------------------------------------------------------------
    // CalculeazaCostComandaAsync (prin CalculeazaCostAsync, privata)
    // ------------------------------------------------------------------

    [Fact]
    public async Task CalculeazaCostComandaAsync_ComandaSubPrag_FaraDiscount_CuTransport()
    {
        await using (var context = _dbFactory.CreateContext())
        {
            await TestDataSeeder.SeedStariComandaAsync(context);
            await TestDataSeeder.SeedConfigurareComandaAsync(context);
        }

        var utilizatorId = await SeedClientAsync();
        var service = CreateService();

        var articole = new List<ArticolCosDto>
        {
            new() { PreparatId = 1, PretUnitar = 25m, Cantitate = 2 }, // subtotal 50
        };

        var calcul = await service.CalculeazaCostComandaAsync(articole, utilizatorId);

        Assert.Equal(50m, calcul.SubtotalMancare);
        Assert.Equal(0m, calcul.ProcentDiscount);
        Assert.Equal(0m, calcul.ValoareDiscount);
        Assert.Null(calcul.MotivDiscount);
        Assert.Equal(15m, calcul.CostTransport); // 50 < PragTransportGratuit (150)
        Assert.Equal(65m, calcul.Total);
        Assert.False(calcul.AreDiscount);
    }

    [Fact]
    public async Task CalculeazaCostComandaAsync_ComandaPestePrag_CuDiscount_FaraTransport()
    {
        await using (var context = _dbFactory.CreateContext())
        {
            await TestDataSeeder.SeedStariComandaAsync(context);
            await TestDataSeeder.SeedConfigurareComandaAsync(context);
        }

        var utilizatorId = await SeedClientAsync();
        var service = CreateService();

        var articole = new List<ArticolCosDto>
        {
            new() { PreparatId = 1, PretUnitar = 200m, Cantitate = 1 }, // subtotal 200
        };

        var calcul = await service.CalculeazaCostComandaAsync(articole, utilizatorId);

        Assert.Equal(200m, calcul.SubtotalMancare);
        Assert.Equal(5m, calcul.ProcentDiscount);
        Assert.Equal(10m, calcul.ValoareDiscount);
        Assert.Contains("Comanda peste", calcul.MotivDiscount);
        Assert.Equal(0m, calcul.CostTransport); // 200 >= PragTransportGratuit (150)
        Assert.Equal(190m, calcul.Total);
        Assert.True(calcul.AreDiscount);
    }

    [Fact]
    public async Task CalculeazaCostComandaAsync_ClientFrecvent_DiscountAplicat()
    {
        await using (var context = _dbFactory.CreateContext())
        {
            await TestDataSeeder.SeedStariComandaAsync(context);
            await TestDataSeeder.SeedConfigurareComandaAsync(context);
        }

        var utilizatorId = await SeedClientAsync();
        await SeedComenziRecenteAsync(utilizatorId, numar: 6); // > NumarComenziPentruDiscount (5)

        var service = CreateService();

        var articole = new List<ArticolCosDto>
        {
            new() { PreparatId = 1, PretUnitar = 25m, Cantitate = 2 }, // subtotal 50, sub pragul de 100
        };

        var calcul = await service.CalculeazaCostComandaAsync(articole, utilizatorId);

        Assert.Equal(50m, calcul.SubtotalMancare);
        Assert.Equal(5m, calcul.ProcentDiscount);
        Assert.Equal(2.5m, calcul.ValoareDiscount);
        Assert.Equal("Client frecvent", calcul.MotivDiscount);
        Assert.Equal(15m, calcul.CostTransport); // 50 < 150, discountul de frecventa nu schimba transportul
        Assert.Equal(62.5m, calcul.Total);
    }

    [Fact]
    public async Task CalculeazaCostComandaAsync_ComandaMareSiClientFrecvent_DiscountNuSeCumuleaza()
    {
        await using (var context = _dbFactory.CreateContext())
        {
            await TestDataSeeder.SeedStariComandaAsync(context);
            await TestDataSeeder.SeedConfigurareComandaAsync(context);
        }

        var utilizatorId = await SeedClientAsync();
        await SeedComenziRecenteAsync(utilizatorId, numar: 6);

        var service = CreateService();

        var articole = new List<ArticolCosDto>
        {
            new() { PreparatId = 1, PretUnitar = 200m, Cantitate = 1 }, // subtotal 200: si comanda mare, si client frecvent
        };

        var calcul = await service.CalculeazaCostComandaAsync(articole, utilizatorId);

        // Ambele conditii sunt indeplinite simultan, dar procentul ramane
        // 5% (ProcentDiscountFrecventa), NU 10% - discountul nu se cumuleaza.
        Assert.Equal(5m, calcul.ProcentDiscount);
        Assert.Equal(10m, calcul.ValoareDiscount);
        Assert.Equal("Comanda peste 100 lei si client frecvent", calcul.MotivDiscount);
    }

    // ------------------------------------------------------------------
    // SchimbaStareComandaAsync (doar tranzitii care nu ating stocul)
    // ------------------------------------------------------------------

    [Fact]
    public async Task SchimbaStareComandaAsync_TranzitieValida_Reuseste()
    {
        _sessionServiceMock.Setup(s => s.EsteAutentificat).Returns(true);
        _sessionServiceMock.Setup(s => s.EsteAngajat).Returns(true);

        int comandaId;
        await using (var context = _dbFactory.CreateContext())
        {
            await TestDataSeeder.SeedStariComandaAsync(context);
        }

        var utilizatorId = await SeedClientAsync();
        var stareInregistrataId = await GetStareIdAsync("inregistrata");

        await using (var context = _dbFactory.CreateContext())
        {
            var comanda = new Comanda
            {
                CodUnic = "CMD-VALID-1",
                UtilizatorId = utilizatorId,
                DataComanda = DateTime.UtcNow,
                StareId = stareInregistrataId,
                CostTransport = 0,
                Discount = 0,
            };
            context.Comenzi.Add(comanda);
            await context.SaveChangesAsync();
            comandaId = comanda.Id;
        }

        var service = CreateService();

        // inregistrata -> anulata: tranzitie valida care NU trece prin "se
        // pregateste", deci nu atinge StoredProcedureRepository.
        var rezultat = await service.SchimbaStareComandaAsync(comandaId, "anulata");

        Assert.True(rezultat.Succes, rezultat.MesajEroare);

        await using var verifyContext = _dbFactory.CreateContext();
        var comandaActualizata = await verifyContext.Comenzi.Include(c => c.Stare).FirstAsync(c => c.Id == comandaId);
        Assert.Equal("anulata", comandaActualizata.Stare.Denumire);
    }

    [Fact]
    public async Task SchimbaStareComandaAsync_TranzitieInvalida_EsteRespinsaCuEroareClara()
    {
        _sessionServiceMock.Setup(s => s.EsteAutentificat).Returns(true);
        _sessionServiceMock.Setup(s => s.EsteAngajat).Returns(true);

        await using (var context = _dbFactory.CreateContext())
        {
            await TestDataSeeder.SeedStariComandaAsync(context);
        }

        var utilizatorId = await SeedClientAsync();
        var stareInregistrataId = await GetStareIdAsync("inregistrata");

        int comandaId;
        await using (var context = _dbFactory.CreateContext())
        {
            var comanda = new Comanda
            {
                CodUnic = "CMD-INVALID-1",
                UtilizatorId = utilizatorId,
                DataComanda = DateTime.UtcNow,
                StareId = stareInregistrataId,
                CostTransport = 0,
                Discount = 0,
            };
            context.Comenzi.Add(comanda);
            await context.SaveChangesAsync();
            comandaId = comanda.Id;
        }

        var service = CreateService();

        // inregistrata -> livrata: NU e o tranzitie valida (trebuie sa treaca
        // intai prin "se pregateste" si "a plecat la client").
        var rezultat = await service.SchimbaStareComandaAsync(comandaId, "livrata");

        Assert.False(rezultat.Succes);
        Assert.Contains("Nu se poate trece direct din starea", rezultat.MesajEroare);

        await using var verifyContext = _dbFactory.CreateContext();
        var comandaNeschimbata = await verifyContext.Comenzi.Include(c => c.Stare).FirstAsync(c => c.Id == comandaId);
        Assert.Equal("inregistrata", comandaNeschimbata.Stare.Denumire);
    }
}
