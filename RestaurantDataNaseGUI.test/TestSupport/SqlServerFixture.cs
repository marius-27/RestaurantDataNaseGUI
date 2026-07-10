using System;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;

namespace RestaurantDataNaseGUI.test.TestSupport;

/// <summary>
/// Connection string catre un SQL Server real (containerul din
/// docker/docker-compose.yml, cu schema deja aplicata - vezi
/// docker/init-db.sh), citit din variabila de mediu
/// RESTAURANT_TEST_CONNECTION_STRING. NU exista o parola implicita
/// hardcodata aici (nici macar una placeholder) - fiecare masina/CI isi
/// seteaza propria variabila inainte de a rula testele de integrare (vezi
/// RestaurantDataNaseGUI.test/README.md).
///
/// Aruncat o singura data per clasa de test care il foloseste (xUnit
/// instantiaza fixture-ul o singura data per colectie, nu per test - vezi
/// <see cref="IntegrationTestCollection"/>), cu un mesaj clar daca variabila
/// lipseste, in loc sa lase testele sa esueze cu o eroare de conexiune
/// greu de inteles.
/// </summary>
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

/// <summary>
/// Toate testele de integrare impart aceeasi colectie xUnit, ca sa ruleze
/// secvential (nu in paralel) impotriva aceleiasi baze de date reale -
/// evita contentie/deadlock-uri intre teste care scriu in acelasi timp in
/// Comanda/Preparat. Testele unitare (Sqlite in-memory, izolate per test)
/// nu sunt afectate - raman in colectiile implicite si ruleaza in paralel.
/// </summary>
[CollectionDefinition("Integration")]
public sealed class IntegrationTestCollection : ICollectionFixture<SqlServerFixture>
{
}
