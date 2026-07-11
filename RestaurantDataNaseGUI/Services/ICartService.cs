using System;
using System.Collections.ObjectModel;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// Cosul de comanda al sesiunii curente - in-memory, ca ISessionService
// (nu persista pe disc, se goleste la logout).
public interface ICartService
{
    ObservableCollection<ArticolCosDto> Articole { get; }

    // Se declanseaza la orice modificare a cosului.
    event EventHandler? CosSchimbat;

    // Adauga un articol deja construit; daca exista deja unul pentru acelasi Preparat/Meniu, ii aduna cantitatea.
    void AdaugaArticol(ArticolCosDto articol);

    // Comoditate: construieste un ArticolCosDto dintr-un item de meniu afisat si il adauga in cos.
    void AdaugaInCos(MeniuAfisareDto item, decimal cantitate = 1m);

    // Seteaza o noua cantitate; sterge articolul daca noua cantitate e &lt;= 0.
    void ModificaCantitate(ArticolCosDto articol, decimal cantitateNoua);

    void StergeArticol(ArticolCosDto articol);

    void GolesteCos();
}
