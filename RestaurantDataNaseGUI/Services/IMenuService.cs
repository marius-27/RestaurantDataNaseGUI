using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// Citirea si cautarea in meniul restaurantului (preparate individuale + meniuri compuse).
public interface IMenuService
{
    // Toate categoriile din meniu, cu preparatele si meniurile aferente.
    // Itemii indisponibili sunt inclusi (nu filtrati), marcati prin EsteIndisponibil.
    Task<List<CategorieMeniuDto>> GetMeniuRestaurantAsync(CancellationToken cancellationToken = default);

    // Filtreaza preparatele si meniurile a caror denumire contine cuvantCheie
    // (case-insensitive), grupate pe categorie ca in GetMeniuRestaurantAsync.
    Task<List<CategorieMeniuDto>> CautaDupaDenumireAsync(string cuvantCheie, CancellationToken cancellationToken = default);

    // Filtreaza dupa un alergen: daca contineAlergen e true, returneaza cele
    // care AU acel alergen; altfel, cele care NU il au deloc (pentru meniuri:
    // niciun preparat component nu il contine). Grupate ca in GetMeniuRestaurantAsync.
    Task<List<CategorieMeniuDto>> CautaDupaAlergenAsync(
        string numeAlergen,
        bool contineAlergen,
        CancellationToken cancellationToken = default);

    // Denumirile tuturor alergenilor din baza de date, sortate alfabetic - pentru un ComboBox de cautare.
    Task<List<string>> GetAlergeniDisponibiliAsync(CancellationToken cancellationToken = default);
}
