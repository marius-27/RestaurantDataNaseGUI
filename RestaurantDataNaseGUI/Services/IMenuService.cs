using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Citirea meniului restaurantului (preparate individuale + meniuri compuse) pentru afisare.</summary>
public interface IMenuService
{
    /// <summary>
    /// Toate categoriile din meniu, fiecare cu preparatele si meniurile
    /// aferente. Itemii indisponibili sunt inclusi (nu filtrati), marcati
    /// prin <see cref="MeniuAfisareDto.EsteIndisponibil"/>.
    /// </summary>
    Task<List<CategorieMeniuDto>> GetMeniuRestaurantAsync(CancellationToken cancellationToken = default);
}
