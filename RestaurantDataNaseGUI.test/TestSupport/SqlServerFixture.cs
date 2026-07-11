using System;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;

namespace RestaurantDataNaseGUI.test.TestSupport;

// Connection string catre SQL Server real (container docker/docker-compose.yml, schema deja aplicata), citit din
// RESTAURANT_TEST_CONNECTION_STRING - fara parola hardcodata; fiecare masina/CI isi seteaza propria variabila.
// Instantiat o singura data per colectie (vezi IntegrationTestCollection); arunca eroare clara daca variabila lipseste.
public sealed class SqlServerFixture
{
    public string ConnectionString { get; }

    public SqlServerFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("RESTAURANT_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Variabila de mediu RESTAURANT_TEST_CONNECTION_STRING nu este setata. " +
                "Testele de integrare (Category=Integration) necesita un SQL Server real, cu " +
                "schema deja aplicata (vezi docker/README.md), si connection string-ul lui. Exemplu:\n\n" +
                "  export RESTAURANT_TEST_CONNECTION_STRING=\"Server=localhost,14330;Database=RestaurantDataNase;User Id=marius;Password=<parola-ta>;TrustServerCertificate=True;\"\n\n" +
                "Pentru a rula doar unit tests, fara aceasta variabila: " +
                "dotnet test --filter \"Category!=Integration\" (vezi RestaurantDataNaseGUI.test/README.md).");
    }

    public RestaurantDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new RestaurantDbContext(options);
    }
}

// Testele de integrare impart aceeasi colectie xUnit, ca sa ruleze secvential (nu in paralel) impotriva aceleiasi baze
// de date reale - evita contentie/deadlock intre teste care scriu in Comanda/Preparat. Testele unitare (Sqlite in-memory) nu sunt afectate.
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<SqlServerFixture>
{
}
