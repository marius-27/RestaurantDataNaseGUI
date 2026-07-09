using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Citirea si cautarea in meniul restaurantului (preparate individuale + meniuri compuse).</summary>
public interface IMenuService
{
    /// <summary>
    /// Toate categoriile din meniu, fiecare cu preparatele si meniurile
    /// aferente. Itemii indisponibili sunt inclusi (nu filtrati), marcati
    /// prin <see cref="MeniuAfisareDto.EsteIndisponibil"/>.
    /// </summary>
    Task<List<CategorieMeniuDto>> GetMeniuRestaurantAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtreaza preparatele si meniurile a caror denumire contine
    /// <paramref name="cuvantCheie"/> (case-insensitive), grupate pe
    /// categorie la fel ca <see cref="GetMeniuRestaurantAsync"/>.
    /// </summary>
    Task<List<CategorieMeniuDto>> CautaDupaDenumireAsync(string cuvantCheie, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtreaza dupa un alergen. Daca <paramref name="contineAlergen"/> e
    /// true, returneaza preparatele/meniurile care AU acel alergen; daca e
    /// false, returneaza cele care NU il au deloc (pentru meniuri: niciun
    /// preparat component nu il contine). Grupate pe categorie la fel ca
    /// <see cref="GetMeniuRestaurantAsync"/>.
    /// </summary>
    Task<List<CategorieMeniuDto>> CautaDupaAlergenAsync(
        string numeAlergen,
        bool contineAlergen,
        CancellationToken cancellationToken = default);

    /// <summary>Denumirile tuturor alergenilor din baza de date, sortate alfabetic - pentru un ComboBox de cautare.</summary>
    Task<List<string>> GetAlergeniDisponibiliAsync(CancellationToken cancellationToken = default);
}
