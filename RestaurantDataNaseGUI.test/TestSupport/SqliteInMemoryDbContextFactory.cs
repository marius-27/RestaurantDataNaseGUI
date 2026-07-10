using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;

namespace RestaurantDataNaseGUI.test.TestSupport;

/// <summary>
/// Baza de date SQLite in-memory, o instanta noua per test (nu partajata):
/// fiecare clasa de test xUnit e instantiata din nou pentru fiecare
/// [Fact]/[Theory], deci un camp readonly initializat in constructor da
/// automat izolare completa intre teste, fara curatare manuala.
///
/// SQLite (nu InMemory provider-ul EF Core) fiindca aplica realmente
/// CHECK-urile si indecsii unici definiti in RestaurantDbContext
/// (OnModelCreating) - provider-ul InMemory le ignora silentios, ceea ce ar
/// face testele sa treaca chiar daca o constrangere reala din
/// database/schema.sql ar fi incalcata.
///
/// O baza SQLite ":memory:" traieste doar cat conexiunea care a creat-o
/// ramane deschisa - de-aia pastram o singura <see cref="SqliteConnection"/>
/// deschisa pe durata testului si construim fiecare RestaurantDbContext nou
/// (prin <see cref="CreateContext"/>) peste ACEEASI conexiune, la fel cum
/// Services/*.cs creeaza cate un DbContext scurt pe apel, prin
/// Func&lt;RestaurantDbContext&gt;, peste aceeasi baza de date reala in productie.
/// </summary>
public sealed class SqliteInMemoryDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<RestaurantDbContext> _options;

    public SqliteInMemoryDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new SqliteTestDbContext(_options);
        context.Database.EnsureCreated();
    }

    // Tipul de retur ramane RestaurantDbContext (ce asteapta Func<RestaurantDbContext>
    // din Services/*.cs) - SqliteTestDbContext e doar un detaliu de
    // implementare, folosit ca sa aplice acelasi model corectat (vezi
    // SqliteTestDbContext) de fiecare data cand un test cere un context nou.
    public RestaurantDbContext CreateContext() => new SqliteTestDbContext(_options);

    public void Dispose() => _connection.Dispose();
}
