using System;
using System.Collections.ObjectModel;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>
/// Cosul de comanda al sesiunii curente - in-memory, simplu, la fel ca
/// ISessionService (nu persista pe disc, se goleste la logout).
/// </summary>
public interface ICartService
{
    ObservableCollection<ArticolCosDto> Articole { get; }

    /// <summary>Se declanseaza la orice modificare a cosului (adaugare, stergere, schimbare cantitate, golire).</summary>
    event EventHandler? CosSchimbat;

    /// <summary>Adauga un articol deja construit; daca exista deja un articol pentru acelasi Preparat/Meniu, ii aduna cantitatea.</summary>
    void AdaugaArticol(ArticolCosDto articol);

    /// <summary>Comoditate: construieste un ArticolCosDto dintr-un item de meniu afisat si il adauga in cos.</summary>
    void AdaugaInCos(MeniuAfisareDto item, decimal cantitate = 1m);

    /// <summary>Seteaza o noua cantitate pentru un articol existent; il sterge daca noua cantitate e &lt;= 0.</summary>
    void ModificaCantitate(ArticolCosDto articol, decimal cantitateNoua);

    void StergeArticol(ArticolCosDto articol);

    void GolesteCos();
}
