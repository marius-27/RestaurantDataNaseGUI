using System;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Tine minte utilizatorul autentificat curent, in memorie, pe durata rularii aplicatiei.</summary>
public interface ISessionService
{
    /// <summary>Utilizatorul autentificat curent, sau null daca nimeni nu e autentificat.</summary>
    Utilizator? CurrentUser { get; }

    bool EsteAutentificat { get; }
    bool EsteAngajat { get; }
    bool EsteClient { get; }

    /// <summary>Se declanseaza la login si la logout, ca ViewModel-urile sa poata reactiona.</summary>
    event EventHandler? CurrentUserChanged;

    void SetCurrentUser(Utilizator utilizator);

    void Logout();
}
