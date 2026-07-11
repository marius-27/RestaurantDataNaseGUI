using System;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.Services;

// Tine minte utilizatorul autentificat curent, in memorie, pe durata rularii aplicatiei.
public interface ISessionService
{
    // Utilizatorul autentificat curent, sau null daca nimeni nu e autentificat.
    Utilizator? CurrentUser { get; }

    bool EsteAutentificat { get; }
    bool EsteAngajat { get; }
    bool EsteClient { get; }

    // Se declanseaza la login si la logout, ca ViewModel-urile sa poata reactiona.
    event EventHandler? CurrentUserChanged;

    void SetCurrentUser(Utilizator utilizator);

    void Logout();
}
