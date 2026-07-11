using System;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.Services;

// Implementare in-memory a ISessionService. Nu exista inca un container DI,
// asa ca Instance e un singleton simplu, comun tuturor ViewModel-urilor;
// cand se introduce DI, aceasta instanta se poate injecta in loc de folosit direct.
public sealed class SessionService : ISessionService
{
    public static SessionService Instance { get; } = new();

    public Utilizator? CurrentUser { get; private set; }

    public bool EsteAutentificat => CurrentUser is not null;
    public bool EsteAngajat => CurrentUser?.TipUtilizator == "Angajat";
    public bool EsteClient => CurrentUser?.TipUtilizator == "Client";

    public event EventHandler? CurrentUserChanged;

    public void SetCurrentUser(Utilizator utilizator)
    {
        CurrentUser = utilizator ?? throw new ArgumentNullException(nameof(utilizator));
        CurrentUserChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Logout()
    {
        CurrentUser = null;
        CurrentUserChanged?.Invoke(this, EventArgs.Empty);
    }
}
