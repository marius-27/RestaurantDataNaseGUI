using System;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.Services;

/// <summary>
/// Implementare in-memory a ISessionService. Proiectul nu are inca un
/// container DI, asa ca <see cref="Instance"/> ofera un singleton simplu pe
/// care orice ViewModel il poate folosi ca sa impartaseasca aceeasi sesiune;
/// cand se introduce navigarea/DI (vezi Services/README.md), aceasta instanta
/// se poate injecta in loc de folosit direct.
/// </summary>
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
