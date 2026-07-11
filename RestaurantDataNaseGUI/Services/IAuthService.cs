using System.Threading;
using System.Threading.Tasks;

namespace RestaurantDataNaseGUI.Services;

// Inregistrare si autentificare pentru clienti, peste RestaurantDbContext.
public interface IAuthService
{
    // Creeaza un Utilizator nou cu TipUtilizator = "Client".
    Task<AuthResult> RegisterAsync(
        string nume,
        string prenume,
        string email,
        string telefon,
        string? adresaLivrare,
        string parola,
        CancellationToken cancellationToken = default);

    // Cauta utilizatorul dupa email si verifica parola.
    Task<AuthResult> LoginAsync(
        string email,
        string parola,
        CancellationToken cancellationToken = default);
}
