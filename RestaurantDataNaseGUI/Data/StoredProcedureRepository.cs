using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Data;

// Apeleaza cele 7 proceduri stocate din schema.sql, prin FromSqlInterpolated
// / ExecuteSqlInterpolatedAsync (parametri reali, niciodata FromSqlRaw cu
// concatenare - evita SQL Injection). Exceptie: CreateComandaAsync foloseste
// ADO.NET direct, fiindca are parametri OUTPUT.
public class StoredProcedureRepository
{
    private readonly RestaurantDbContext _context;

    public StoredProcedureRepository(RestaurantDbContext context)
    {
        _context = context;
    }

    // dbo.sp_CreateComanda - creeaza antetul comenzii, returneaza Id si cod
    // unic (parametri OUTPUT).
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

    // dbo.sp_AdaugaDetaliuComanda - adauga o linie in comanda; exact unul
    // dintre preparatId/meniuId trebuie completat.
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

    // dbo.sp_UpdateCantitateTotalaLaComanda - scade din stoc cantitatile
    // comenzii (preparate directe + din meniuri).
    public async Task UpdateCantitateTotalaLaComandaAsync(int comandaId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.sp_UpdateCantitateTotalaLaComanda @ComandaId = {comandaId}",
            cancellationToken);
    }

    // dbo.sp_GetComenziClientCuDetalii - comenzile unui client, cu liniile
    // de detaliu si subtotalul fiecareia.
    public async Task<List<ComenziClientDetaliuDto>> GetComenziClientCuDetaliiAsync(
        int utilizatorId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ComenziClientDetalii
            .FromSqlInterpolated($"EXEC dbo.sp_GetComenziClientCuDetalii @UtilizatorId = {utilizatorId}")
            .ToListAsync(cancellationToken);
    }

    // dbo.sp_GetPreparateApropiateDeEpuizare - preparate cu stoc sub prag;
    // daca pragCantitate e null, foloseste pragul din dbo.Configurare
    // (cheia PragStocEpuizare).
    public async Task<List<PreparatEpuizareDto>> GetPreparateApropiateDeEpuizareAsync(
        decimal? pragCantitate = null,
        CancellationToken cancellationToken = default)
    {
        return await _context.PreparateApropiateDeEpuizare
            .FromSqlInterpolated($"EXEC dbo.sp_GetPreparateApropiateDeEpuizare @PragCantitate = {pragCantitate}")
            .ToListAsync(cancellationToken);
    }

    // dbo.sp_GetMeniuRestaurantCuAlergeni - meniurile, cu pret calculat
    // dinamic si alergenii agregati din preparatele componente.
    public async Task<List<MeniuCuAlergeniDto>> GetMeniuRestaurantCuAlergeniAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.MeniuriCuAlergeni
            .FromSqlInterpolated($"EXEC dbo.sp_GetMeniuRestaurantCuAlergeni")
            .ToListAsync(cancellationToken);
    }

    // dbo.sp_SetPreparatIndisponibil - soft-delete: marcheaza preparatul
    // indisponibil in loc sa-l stearga.
    public async Task SetPreparatIndisponibilAsync(int preparatId, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC dbo.sp_SetPreparatIndisponibil @PreparatId = {preparatId}",
            cancellationToken);
    }
}
