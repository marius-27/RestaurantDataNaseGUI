using System.Threading;
using System.Threading.Tasks;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Inregistrare si autentificare pentru clienti, peste RestaurantDbContext.</summary>
public interface IAuthService
{
    /// <summary>Creeaza un Utilizator nou cu TipUtilizator = "Client".</summary>
    Task<AuthResult> RegisterAsync(
        string nume,
        string prenume,
        string email,
        string telefon,
        string? adresaLivrare,
        string parola,
        CancellationToken cancellationToken = default);

    /// <summary>Cauta utilizatorul dupa email si verifica parola.</summary>
    Task<AuthResult> LoginAsync(
        string email,
        string parola,
        CancellationToken cancellationToken = default);
}
