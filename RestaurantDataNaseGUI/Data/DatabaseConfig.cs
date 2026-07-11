using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace RestaurantDataNaseGUI.Data;

// Configuratia si optiunile pentru RestaurantDbContext, din appsettings.json
// - niciodata hardcodate in cod.
public static class DatabaseConfig
{
    private const string ConnectionStringName = "RestaurantDataNase";

    // Incarca appsettings.json si, daca exista, appsettings.Development.json.
    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .Build();
    }

    public static string GetConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' nu a fost gasit in appsettings.json.");
    }

    public static DbContextOptions<RestaurantDbContext> CreateDbContextOptions(IConfiguration? configuration = null)
    {
        configuration ??= BuildConfiguration();
        var connectionString = GetConnectionString(configuration);

        return new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    // Creeaza direct un RestaurantDbContext, fara container DI.
    public static RestaurantDbContext CreateDbContext(IConfiguration? configuration = null)
    {
        return new RestaurantDbContext(CreateDbContextOptions(configuration));
    }
}
