using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace RestaurantDataNaseGUI.Data;

/// <summary>
/// Construieste configuratia si optiunile pentru RestaurantDbContext pe baza
/// connection string-ului din appsettings.json - niciodata hardcodat in cod.
/// </summary>
public static class DatabaseConfig
{
    private const string ConnectionStringName = "RestaurantDataNase";

    /// <summary>
    /// Incarca appsettings.json (si, daca exista, appsettings.Development.json)
    /// din folderul aplicatiei.
    /// </summary>
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

    /// <summary>Creeaza direct un RestaurantDbContext, fara a necesita un container DI.</summary>
    public static RestaurantDbContext CreateDbContext(IConfiguration? configuration = null)
    {
        return new RestaurantDbContext(CreateDbContextOptions(configuration));
    }
}
