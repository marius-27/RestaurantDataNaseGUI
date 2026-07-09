using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Rezultatul unei operatii de autentificare/inregistrare din <see cref="IAuthService"/>.</summary>
public sealed class AuthResult
{
    public bool Succes { get; }
    public Utilizator? Utilizator { get; }
    public string? MesajEroare { get; }

    private AuthResult(bool succes, Utilizator? utilizator, string? mesajEroare)
    {
        Succes = succes;
        Utilizator = utilizator;
        MesajEroare = mesajEroare;
    }

    public static AuthResult Ok(Utilizator utilizator) => new(true, utilizator, null);

    public static AuthResult Esec(string mesajEroare) => new(false, null, mesajEroare);
}
