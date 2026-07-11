using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// Vizualizarea stocului aproape de epuizare - doar pentru angajati autentificati.
public interface IStockService
{
    // Preparatele cu stoc sub pragul din dbo.Configurare (PragStocEpuizare) -
    // doar pentru angajati autentificati (altfel arunca UnauthorizedAccessException).
    Task<List<PreparatEpuizareDto>> GetPreparateApropiateDeEpuizareAsync(CancellationToken cancellationToken = default);
}
