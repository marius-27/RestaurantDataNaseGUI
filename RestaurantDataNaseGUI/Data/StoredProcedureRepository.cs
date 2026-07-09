using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Data;

/// <summary>
/// Apeleaza cele 7 proceduri stocate din database/schema.sql. Toate metodele
/// folosesc FromSqlInterpolated / ExecuteSqlInterpolatedAsync (parametri
/// interpolati -> DbParameter reali), niciodata FromSqlRaw cu concatenare de
/// string, pentru a evita SQL Injection. Singura exceptie este
/// CreateComandaAsync, care are parametri OUTPUT si de aceea foloseste
/// ADO.NET direct (SqlCommand + SqlParameter), fiindca FromSqlInterpolated
/// nu suporta parametri OUTPUT.
/// </summary>
public class StoredProcedureRepository
{
    private readonly RestaurantDbContext _context;

    public StoredProcedureRepository(RestaurantDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// dbo.sp_CreateComanda - creeaza antetul unei comenzi noi si returneaza
    /// Id-ul generat si codul unic (parametri OUTPUT in procedura).
    /// </summary>
    public async Task<(int ComandaId, string CodUnic)> CreateComandaAsync(
        int utilizatorId,
        decimal costTransport = 0m,
        decimal discount = 0m,
        CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)_context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_CreateComanda";
            command.CommandType = CommandType.StoredProcedure;

            if (_context.Database.CurrentTransaction is { } currentTransaction)
            {
                command.Transaction = (SqlTransaction)currentTransaction.GetDbTransaction();
            }

            command.Parameters.Add(new SqlParameter("@UtilizatorId", SqlDbType.Int) { Value = utilizatorId });
            command.Parameters.Add(new SqlParameter("@CostTransport", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = costTransport });
            command.Parameters.Add(new SqlParameter("@Discount", SqlDbType.Decimal) { Precision = 5, Scale = 2, Value = discount });

            var comandaIdParam = new SqlParameter("@ComandaId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var codUnicParam = new SqlParameter("@CodUnic", SqlDbType.VarChar, 20) { Direction = ParameterDirection.Output };
            command.Parameters.Add(comandaIdParam);
            command.Parameters.Add(codUnicParam);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var comandaId = (int)comandaIdParam.Value!;
            var codUnic = (string)codUnicParam.Value!;
            return (comandaId, codUnic);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    /// <summary>
    /// dbo.sp_AdaugaDetaliuComanda - adauga o linie intr-o comanda existenta.
    /// Trebuie specificat exact unul dintre preparatId / meniuId.
    /// </summary>
    public async Task AdaugaDetaliuComandaAsync(
        int comandaId,
        int? preparatId,
        int? meniuId,
        decimal cantitate,
        CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.sp_AdaugaDetaliuComanda @ComandaId = {comandaId}, @PreparatId = {preparatId}, @MeniuId = {meniuId}, @Cantitate = {cantitate}",
            cancellationToken);
    }

    /// <summary>
    /// dbo.sp_UpdateCantitateTotalaLaComanda - scade din stoc cantitatile
    /// consumate de o comanda (preparate directe + preparate din meniuri).
    /// </summary>
    public async Task UpdateCantitateTotalaLaComandaAsync(int comandaId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.sp_UpdateCantitateTotalaLaComanda @ComandaId = {comandaId}",
            cancellationToken);
    }

    /// <summary>
    /// dbo.sp_GetComenziClientCuDetalii [interogare complexa] - toate
    /// comenzile unui client, cu liniile de detaliu si subtotalul fiecareia.
    /// </summary>
    public async Task<List<ComenziClientDetaliuDto>> GetComenziClientCuDetaliiAsync(
        int utilizatorId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ComenziClientDetalii
            .FromSqlInterpolated($"EXEC dbo.sp_GetComenziClientCuDetalii @UtilizatorId = {utilizatorId}")
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// dbo.sp_GetPreparateApropiateDeEpuizare - preparatele cu stoc sub prag.
    /// Daca <paramref name="pragCantitate"/> este null, procedura preia
    /// pragul implicit din dbo.Configurare (cheia PragStocEpuizare).
    /// </summary>
    public async Task<List<PreparatEpuizareDto>> GetPreparateApropiateDeEpuizareAsync(
        decimal? pragCantitate = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.PreparateApropiateDeEpuizare
            .FromSqlInterpolated($"EXEC dbo.sp_GetPreparateApropiateDeEpuizare @PragCantitate = {pragCantitate}")
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// dbo.sp_GetMeniuRestaurantCuAlergeni [interogare complexa] - toate
    /// meniurile, cu pretul calculat dinamic si alergenii agregati din
    /// toate preparatele componente.
    /// </summary>
    public async Task<List<MeniuCuAlergeniDto>> GetMeniuRestaurantCuAlergeniAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.MeniuriCuAlergeni
            .FromSqlInterpolated($"EXEC dbo.sp_GetMeniuRestaurantCuAlergeni")
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// dbo.sp_SetPreparatIndisponibil - soft-delete: marcheaza un preparat ca
    /// indisponibil (Disponibil = 0) in loc sa il stearga fizic.
    /// </summary>
    public async Task SetPreparatIndisponibilAsync(int preparatId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.sp_SetPreparatIndisponibil @PreparatId = {preparatId}",
            cancellationToken);
    }
}
