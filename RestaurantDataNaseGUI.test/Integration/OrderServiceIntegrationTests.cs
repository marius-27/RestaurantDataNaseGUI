using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;
using RestaurantDataNaseGUI.test.TestSupport;
using Xunit;

namespace RestaurantDataNaseGUI.test.Integration;

/// <summary>
/// Teste de integrare pentru OrderService, impotriva unui SQL Server real
/// (vezi RestaurantDataNaseGUI.test/README.md pentru cum se configureaza).
/// Acopera exact caile care ating StoredProcedureRepository (deci proceduri
/// stocate T-SQL reale) - imposibil de testat cu SQLite/InMemory, vezi
/// notele din Services/OrderServiceTests.cs.
///
/// Fiecare test isi creeaza propriile date (Categorie/Preparat/Utilizator cu
/// nume unice, sufixate cu un Guid) si le sterge la final, ca sa nu lase
/// reziduuri in baza de date folosita si de aplicatie in dezvoltare locala.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class OrderServiceIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    public OrderServiceIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    private static Mock<ISessionService> CreateClientSessionMock(Utilizator utilizator)
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.EsteAutentificat).Returns(true);
        mock.Setup(s => s.EsteClient).Returns(true);
        mock.Setup(s => s.CurrentUser).Returns(utilizator);
        return mock;
    }

    private static Mock<ISessionService> CreateAngajatSessionMock()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.EsteAutentificat).Returns(true);
        mock.Setup(s => s.EsteAngajat).Returns(true);
        return mock;
    }

    [Fact]
    public async Task CreeazaComandaAsync_ScrieInBazaDeDate_ComandaSiDetaliiCreateCorect()
    {
        var sufix = Guid.NewGuid().ToString("N")[..8];
        await using var context = _fixture.CreateContext();

        var categorie = new Categorie { Denumire = $"Categorie-Test-{sufix}" };
        var preparat = new Preparat
        {
            Denumire = $"Preparat-Test-{sufix}",
            Pret = 30m,
            CantitatePortie = 300m,
            UnitateMasura = "g",
            CantitateTotalaRestaurant = 100m,
            Disponibil = true,
        };
        categorie.Preparate.Add(preparat);

        var utilizator = new Utilizator
        {
            Nume = "Test",
            Prenume = "Integrare",
            Email = $"integrare-{sufix}@test.ro",
            Telefon = "0700000000",
            ParolaHash = "hash-nu-conteaza",
            TipUtilizator = "Client",
        };

        context.Categorii.Add(categorie);
        context.Utilizatori.Add(utilizator);
        await context.SaveChangesAsync();

        try
        {
            var sessionMock = CreateClientSessionMock(utilizator);
            var orderService = new OrderService(sessionMock.Object, _fixture.CreateContext);

            var articole = new List<ArticolCosDto>
            {
                new() { PreparatId = preparat.Id, PretUnitar = preparat.Pret, Cantitate = 2 },
            };

            var rezultat = await orderService.CreeazaComandaAsync(articole, utilizator.Id);

            Assert.True(rezultat.Succes, rezultat.MesajEroare);
            Assert.NotNull(rezultat.ComandaId);
            Assert.NotNull(rezultat.CodUnic);

            await using var verifyContext = _fixture.CreateContext();
            var comandaCreata = await verifyContext.Comenzi
                .Include(c => c.ComandaDetalii)
                .FirstOrDefaultAsync(c => c.Id == rezultat.ComandaId);

            Assert.NotNull(comandaCreata);
            Assert.Equal(utilizator.Id, comandaCreata!.UtilizatorId);
            Assert.Equal(rezultat.CodUnic, comandaCreata.CodUnic);

            var detaliu = Assert.Single(comandaCreata.ComandaDetalii);
            Assert.Equal(preparat.Id, detaliu.PreparatId);
            Assert.Null(detaliu.MeniuId);
            Assert.Equal(2m, detaliu.Cantitate);
            Assert.Equal(preparat.Pret, detaliu.PretUnitarLaComanda);
        }
        finally
        {
            await CleanupAsync(utilizatorId: utilizator.Id, preparatId: preparat.Id, categorieId: categorie.Id);
        }
    }

    [Fact]
    public async Task SchimbaStareComandaAsync_CatreSePregateste_ScadeStocul()
    {
        var sufix = Guid.NewGuid().ToString("N")[..8];
        await using var context = _fixture.CreateContext();

        var categorie = new Categorie { Denumire = $"Categorie-Test-{sufix}" };
        var preparat = new Preparat
        {
            Denumire = $"Preparat-Test-{sufix}",
            Pret = 10m,
            CantitatePortie = 100m,
            UnitateMasura = "buc",
            CantitateTotalaRestaurant = 100m, // stoc initial
            Disponibil = true,
        };
        categorie.Preparate.Add(preparat);

        var utilizator = new Utilizator
        {
            Nume = "Test",
            Prenume = "Integrare",
            Email = $"integrare-{sufix}@test.ro",
            Telefon = "0700000000",
            ParolaHash = "hash-nu-conteaza",
            TipUtilizator = "Client",
        };

        context.Categorii.Add(categorie);
        context.Utilizatori.Add(utilizator);
        await context.SaveChangesAsync();

        try
        {
            var clientSessionMock = CreateClientSessionMock(utilizator);
            var orderServiceClient = new OrderService(clientSessionMock.Object, _fixture.CreateContext);

            var articole = new List<ArticolCosDto>
            {
                new() { PreparatId = preparat.Id, PretUnitar = preparat.Pret, Cantitate = 10 }, // consuma 10 din 100
            };

            var creare = await orderServiceClient.CreeazaComandaAsync(articole, utilizator.Id);
            Assert.True(creare.Succes, creare.MesajEroare);

            var angajatSessionMock = CreateAngajatSessionMock();
            var orderServiceAngajat = new OrderService(angajatSessionMock.Object, _fixture.CreateContext);

            var schimbare = await orderServiceAngajat.SchimbaStareComandaAsync(creare.ComandaId!.Value, "se pregateste");

            Assert.True(schimbare.Succes, schimbare.MesajEroare);

            await using var verifyContext = _fixture.CreateContext();
            var preparatActualizat = await verifyContext.Preparate.FirstAsync(p => p.Id == preparat.Id);
            Assert.Equal(90m, preparatActualizat.CantitateTotalaRestaurant); // 100 - 10

            var comandaActualizata = await verifyContext.Comenzi
                .Include(c => c.Stare)
                .FirstAsync(c => c.Id == creare.ComandaId!.Value);
            Assert.Equal("se pregateste", comandaActualizata.Stare.Denumire);
        }
        finally
        {
            await CleanupAsync(utilizatorId: utilizator.Id, preparatId: preparat.Id, categorieId: categorie.Id);
        }
    }

    [Fact]
    public async Task SchimbaStareComandaAsync_CatreSePregateste_StocInsuficient_Rollback()
    {
        var sufix = Guid.NewGuid().ToString("N")[..8];
        await using var context = _fixture.CreateContext();

        var categorie = new Categorie { Denumire = $"Categorie-Test-{sufix}" };
        var preparat = new Preparat
        {
            Denumire = $"Preparat-Test-{sufix}",
            Pret = 10m,
            CantitatePortie = 100m,
            UnitateMasura = "buc",
            CantitateTotalaRestaurant = 5m, // stoc initial - insuficient pentru comanda de mai jos
            Disponibil = true,
        };
        categorie.Preparate.Add(preparat);

        var utilizator = new Utilizator
        {
            Nume = "Test",
            Prenume = "Integrare",
            Email = $"integrare-{sufix}@test.ro",
            Telefon = "0700000000",
            ParolaHash = "hash-nu-conteaza",
            TipUtilizator = "Client",
        };

        context.Categorii.Add(categorie);
        context.Utilizatori.Add(utilizator);
        await context.SaveChangesAsync();

        try
        {
            var clientSessionMock = CreateClientSessionMock(utilizator);
            var orderServiceClient = new OrderService(clientSessionMock.Object, _fixture.CreateContext);

            var articole = new List<ArticolCosDto>
            {
                // Comanda 10 bucati dintr-un preparat cu doar 5 in stoc -
                // creare comenzii reuseste (VerificaDisponibilitateaAsync
                // verifica doar Disponibil, nu cantitatea), dar stocul devine
                // negativ la "se pregateste".
                new() { PreparatId = preparat.Id, PretUnitar = preparat.Pret, Cantitate = 10 },
            };

            var creare = await orderServiceClient.CreeazaComandaAsync(articole, utilizator.Id);
            Assert.True(creare.Succes, creare.MesajEroare);

            var angajatSessionMock = CreateAngajatSessionMock();
            var orderServiceAngajat = new OrderService(angajatSessionMock.Object, _fixture.CreateContext);

            var schimbare = await orderServiceAngajat.SchimbaStareComandaAsync(creare.ComandaId!.Value, "se pregateste");

            Assert.False(schimbare.Succes);
            Assert.Contains("stocului insuficient", schimbare.MesajEroare);

            await using var verifyContext = _fixture.CreateContext();

            var preparatNeschimbat = await verifyContext.Preparate.FirstAsync(p => p.Id == preparat.Id);
            Assert.Equal(5m, preparatNeschimbat.CantitateTotalaRestaurant); // neschimbat - rollback

            var comandaNeschimbata = await verifyContext.Comenzi
                .Include(c => c.Stare)
                .FirstAsync(c => c.Id == creare.ComandaId!.Value);
            Assert.Equal("inregistrata", comandaNeschimbata.Stare.Denumire); // neschimbata - rollback
        }
        finally
        {
            await CleanupAsync(utilizatorId: utilizator.Id, preparatId: preparat.Id, categorieId: categorie.Id);
        }
    }

    private async Task CleanupAsync(int utilizatorId, int preparatId, int categorieId)
    {
        await using var context = _fixture.CreateContext();

        var comenzi = await context.Comenzi
            .Include(c => c.ComandaDetalii)
            .Where(c => c.UtilizatorId == utilizatorId)
            .ToListAsync();

        foreach (var comanda in comenzi)
        {
            context.ComandaDetalii.RemoveRange(comanda.ComandaDetalii);
        }

        context.Comenzi.RemoveRange(comenzi);
        await context.SaveChangesAsync();

        var preparat = await context.Preparate.FindAsync(preparatId);
        if (preparat is not null)
        {
            context.Preparate.Remove(preparat);
        }

        var utilizator = await context.Utilizatori.FindAsync(utilizatorId);
        if (utilizator is not null)
        {
            context.Utilizatori.Remove(utilizator);
        }

        await context.SaveChangesAsync();

        var categorie = await context.Categorii.FindAsync(categorieId);
        if (categorie is not null)
        {
            context.Categorii.Remove(categorie);
            await context.SaveChangesAsync();
        }
    }
}
