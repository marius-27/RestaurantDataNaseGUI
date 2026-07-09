using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Implementare IStockService, separata de AdminService fiindca nu e CRUD - doar citire de stoc.</summary>
public class StockService : IStockService
{
    private readonly ISessionService _sessionService;
    private readonly Func<RestaurantDbContext> _dbContextFactory;

    public StockService(ISessionService? sessionService = null, Func<RestaurantDbContext>? dbContextFactory = null)
    {
        _sessionService = sessionService ?? SessionService.Instance;
        _dbContextFactory = dbContextFactory ?? (() => DatabaseConfig.CreateDbContext());
    }

    public async Task<List<PreparatEpuizareDto>> GetPreparateApropiateDeEpuizareAsync(CancellationToken cancellationToken = default)
    {
        if (!_sessionService.EsteAutentificat || !_sessionService.EsteAngajat)
        {
            throw new UnauthorizedAccessException("Aceasta actiune este permisa doar angajatilor autentificati.");
        }

        await using var context = _dbContextFactory();
        var repository = new StoredProcedureRepository(context);

        // Fara parametru explicit - procedura preia pragul implicit din
        // dbo.Configurare (cheia PragStocEpuizare).
        return await repository.GetPreparateApropiateDeEpuizareAsync(pragCantitate: null, cancellationToken: cancellationToken);
    }
}
