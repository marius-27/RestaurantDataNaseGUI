using System;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Services;
using RestaurantDataNaseGUI.test.TestSupport;
using Xunit;

namespace RestaurantDataNaseGUI.test.Services;

public sealed class AuthServiceTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory _dbFactory = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(_dbFactory.CreateContext);
    }

    public void Dispose() => _dbFactory.Dispose();

    [Fact]
    public async Task RegisterAsync_ParolaPreaScurta_EsteRespinsa()
    {
        var rezultat = await _authService.RegisterAsync(
            "Popescu", "Ion", "ion.popescu@example.com", "0712345678", null, "scurt1");

        Assert.False(rezultat.Succes);
        Assert.Contains("minim 8 caractere", rezultat.MesajEroare);
        Assert.Null(rezultat.Utilizator);
    }

    [Fact]
    public async Task RegisterAsync_EmailInvalid_EsteRespins()
    {
        var rezultat = await _authService.RegisterAsync(
            "Popescu", "Ion", "nu-e-un-email", "0712345678", null, "ParolaBuna123!");

        Assert.False(rezultat.Succes);
        Assert.Contains("email", rezultat.MesajEroare, StringComparison.OrdinalIgnoreCase);
        Assert.Null(rezultat.Utilizator);
    }

    [Fact]
    public async Task RegisterAsync_Succes_HashParoleiNuEsteParolaInClar()
    {
        const string parola = "ParolaBuna123!";

        var rezultat = await _authService.RegisterAsync(
            "Popescu", "Ion", "ion.popescu@example.com", "0712345678", null, parola);

        Assert.True(rezultat.Succes);
        Assert.NotNull(rezultat.Utilizator);
        Assert.NotEqual(parola, rezultat.Utilizator!.ParolaHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(parola, rezultat.Utilizator.ParolaHash));
    }

    [Fact]
    public async Task LoginAsync_ParolaGresita_Esueaza()
    {
        await _authService.RegisterAsync(
            "Popescu", "Ion", "ion.popescu@example.com", "0712345678", null, "ParolaCorecta123!");

        var rezultat = await _authService.LoginAsync("ion.popescu@example.com", "AltaParola456!");

        Assert.False(rezultat.Succes);
        Assert.Null(rezultat.Utilizator);
        Assert.Equal("Email sau parola incorecta.", rezultat.MesajEroare);
    }

    [Fact]
    public async Task LoginAsync_ParolaCorecta_Reuseste()
    {
        await _authService.RegisterAsync(
            "Popescu", "Ion", "ion.popescu@example.com", "0712345678", null, "ParolaCorecta123!");

        var rezultat = await _authService.LoginAsync("ion.popescu@example.com", "ParolaCorecta123!");

        Assert.True(rezultat.Succes);
        Assert.NotNull(rezultat.Utilizator);
        Assert.Equal("ion.popescu@example.com", rezultat.Utilizator!.Email);
        Assert.IsType<Utilizator>(rezultat.Utilizator);
    }
}
