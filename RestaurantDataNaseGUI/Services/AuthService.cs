using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.Services;

// Implementare IAuthService peste RestaurantDbContext. Fiecare operatie deschide
// propriul DbContext scurt-traitor (via _dbContextFactory), conform recomandarii EF
// Core pentru desktop - DbContext nu e thread-safe si nu trebuie tinut deschis.
public class AuthService : IAuthService
{
    private const int LungimeMinimaParola = 8;
    private const string TipUtilizatorClient = "Client";

    private const int SqlErrorUniqueConstraint = 2627;
    private const int SqlErrorUniqueIndex = 2601;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly Func<RestaurantDbContext> _dbContextFactory;

    public AuthService(Func<RestaurantDbContext>? dbContextFactory = null)
    {
        _dbContextFactory = dbContextFactory ?? (() => DatabaseConfig.CreateDbContext());
    }

    public async Task<AuthResult> RegisterAsync(
        string nume,
        string prenume,
        string email,
        string telefon,
        string? adresaLivrare,
        string parola,
        CancellationToken cancellationToken = default)
    {
        nume = (nume ?? string.Empty).Trim();
        prenume = (prenume ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim();
        telefon = (telefon ?? string.Empty).Trim();
        adresaLivrare = string.IsNullOrWhiteSpace(adresaLivrare) ? null : adresaLivrare.Trim();
        parola ??= string.Empty;

        if (string.IsNullOrWhiteSpace(nume) || string.IsNullOrWhiteSpace(prenume))
        {
            return AuthResult.Esec("Numele si prenumele sunt obligatorii.");
        }

        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
        {
            return AuthResult.Esec("Adresa de email nu este valida.");
        }

        if (string.IsNullOrWhiteSpace(telefon))
        {
            return AuthResult.Esec("Numarul de telefon este obligatoriu.");
        }

        if (parola.Length < LungimeMinimaParola)
        {
            return AuthResult.Esec($"Parola trebuie sa aiba minim {LungimeMinimaParola} caractere.");
        }

        await using var context = _dbContextFactory();

        var emailExistent = await context.Utilizatori
            .AnyAsync(u => u.Email == email, cancellationToken);

        if (emailExistent)
        {
            return AuthResult.Esec("Exista deja un cont inregistrat cu acest email.");
        }

        var utilizator = new Utilizator
        {
            Nume = nume,
            Prenume = prenume,
            Email = email,
            Telefon = telefon,
            AdresaLivrare = adresaLivrare,
            ParolaHash = BCrypt.Net.BCrypt.HashPassword(parola),
            TipUtilizator = TipUtilizatorClient,
        };

        context.Utilizatori.Add(utilizator);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (EsteIncalcareUnicitateEmail(ex))
        {
            // Fallback pentru cursa dintre verificarea AnyAsync de mai sus si
            // insert - alt request poate inregistra acelasi email intre timp.
            return AuthResult.Esec("Exista deja un cont inregistrat cu acest email.");
        }

        return AuthResult.Ok(utilizator);
    }

    public async Task<AuthResult> LoginAsync(
        string email,
        string parola,
        CancellationToken cancellationToken = default)
    {
        email = (email ?? string.Empty).Trim();
        parola ??= string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(parola))
        {
            return AuthResult.Esec("Email si parola sunt obligatorii.");
        }

        await using var context = _dbContextFactory();

        var utilizator = await context.Utilizatori
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (utilizator is null || !BCrypt.Net.BCrypt.Verify(parola, utilizator.ParolaHash))
        {
            return AuthResult.Esec("Email sau parola incorecta.");
        }

        return AuthResult.Ok(utilizator);
    }

    private static bool EsteIncalcareUnicitateEmail(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == SqlErrorUniqueConstraint || sqlEx.Number == SqlErrorUniqueIndex);
    }
}
