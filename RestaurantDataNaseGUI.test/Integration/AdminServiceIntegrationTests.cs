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
/// Completeaza Services/AdminServiceTests.cs cu scenariul care NU e
/// testabil pe SQLite: StergePreparatAsync, cand preparatul a fost deja
/// folosit intr-o comanda, apeleaza
/// StoredProcedureRepository.SetPreparatIndisponibilAsync - "EXEC dbo.sp_SetPreparatIndisponibil",
/// o procedura stocata T-SQL reala. Vezi nota din AdminServiceTests.cs.
/// </summary>
[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class AdminServiceIntegrationTests
{
    private readonly SqlServerFixture _fixture;

    public AdminServiceIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task StergePreparatAsync_FolositIntrOComanda_DeclanseazaSoftDelete()
    {
        var sufix = Guid.NewGuid().ToString("N")[..8];
        await using var context = _fixture.CreateContext();

        var categorie = new Categorie { Denumire = $"Categorie-Test-{sufix}" };
        var preparat = new Preparat
        {
            Denumire = $"Preparat-Test-{sufix}",
            Pret = 18m,
            CantitatePortie = 250m,
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
            // Preparatul trebuie folosit intr-o comanda existenta, ca sa
            // declanseze ramura de soft-delete din StergePreparatAsync.
            var clientSessionMock = new Mock<ISessionService>();
            clientSessionMock.Setup(s => s.EsteAutentificat).Returns(true);
            clientSessionMock.Setup(s => s.EsteClient).Returns(true);
            clientSessionMock.Setup(s => s.CurrentUser).Returns(utilizator);

            var orderService = new OrderService(clientSessionMock.Object, _fixture.CreateContext);
            var articole = new List<ArticolCosDto>
            {
                new() { PreparatId = preparat.Id, PretUnitar = preparat.Pret, Cantitate = 1 },
            };
            var creare = await orderService.CreeazaComandaAsync(articole, utilizator.Id);
            Assert.True(creare.Succes, creare.MesajEroare);

            var angajatSessionMock = new Mock<ISessionService>();
            angajatSessionMock.Setup(s => s.EsteAutentificat).Returns(true);
            angajatSessionMock.Setup(s => s.EsteAngajat).Returns(true);

            var adminService = new AdminService(angajatSessionMock.Object, _fixture.CreateContext);
            var rezultat = await adminService.StergePreparatAsync(preparat.Id);

            Assert.True(rezultat.Succes, rezultat.MesajEroare);

            await using var verifyContext = _fixture.CreateContext();
            var preparatActualizat = await verifyContext.Preparate.FirstOrDefaultAsync(p => p.Id == preparat.Id);

            Assert.NotNull(preparatActualizat); // NU a fost sters fizic
            Assert.False(preparatActualizat!.Disponibil); // soft-delete
        }
        finally
        {
            await using var cleanupContext = _fixture.CreateContext();

            var comenzi = await cleanupContext.Comenzi
                .Include(c => c.ComandaDetalii)
                .Where(c => c.UtilizatorId == utilizator.Id)
                .ToListAsync();

            foreach (var comanda in comenzi)
            {
                cleanupContext.ComandaDetalii.RemoveRange(comanda.ComandaDetalii);
            }

            cleanupContext.Comenzi.RemoveRange(comenzi);
            await cleanupContext.SaveChangesAsync();

            var p = await cleanupContext.Preparate.FindAsync(preparat.Id);
            if (p is not null) cleanupContext.Preparate.Remove(p);

            var u = await cleanupContext.Utilizatori.FindAsync(utilizator.Id);
            if (u is not null) cleanupContext.Utilizatori.Remove(u);

            await cleanupContext.SaveChangesAsync();

            var c = await cleanupContext.Categorii.FindAsync(categorie.Id);
            if (c is not null) cleanupContext.Categorii.Remove(c);
            await cleanupContext.SaveChangesAsync();
        }
    }
}
