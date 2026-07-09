using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Vizualizarea stocului aproape de epuizare - doar pentru angajati autentificati.</summary>
public interface IStockService
{
    /// <summary>
    /// Preparatele cu stoc sub pragul din dbo.Configurare (cheia
    /// PragStocEpuizare) - doar pentru angajati autentificati (arunca
    /// UnauthorizedAccessException altfel).
    /// </summary>
    Task<List<PreparatEpuizareDto>> GetPreparateApropiateDeEpuizareAsync(CancellationToken cancellationToken = default);
}
