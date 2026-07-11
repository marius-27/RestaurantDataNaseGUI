using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;

namespace RestaurantDataNaseGUI.test.TestSupport;

// Baza SQLite in-memory, instanta noua per test - izolare completa, fara curatare manuala.
// Foloseste SQLite (nu InMemory provider-ul EF Core) ca sa aplice realmente CHECK-urile si indecsii unici din
// OnModelCreating, pe care InMemory le-ar ignora silentios. Conexiunea ":memory:" trebuie tinuta deschisa; CreateContext
// creeaza cate un DbContext nou peste aceeasi conexiune, la fel ca Func<RestaurantDbContext> in productie.
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
