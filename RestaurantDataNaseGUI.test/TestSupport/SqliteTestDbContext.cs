using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.test.TestSupport;

/// <summary>
/// Subclasa RestaurantDbContext folosita DOAR de testele unitare (SQLite
/// in-memory) - RestaurantDbContext.cs din proiectul principal ramane
/// neatins. Motiv: CK_Utilizator_TipUtilizator e definit in
/// RestaurantDbContext cu sintaxa T-SQL "N'Client'"/"N'Angajat'" (prefixul N
/// = literal Unicode in SQL Server) - SQLite nu recunoaste acest prefix si
/// EnsureCreated() esueaza cu "near 'Client': syntax error" la CREATE TABLE.
/// Redeclaram aici acelasi CHECK, cu sintaxa portabila (fara N) - identic
/// din punct de vedere functional pe SQL Server, pentru cele doua valori
/// ASCII simple ("Client"/"Angajat"), dar inteles si de SQLite.
/// </summary>
internal sealed class SqliteTestDbContext : RestaurantDbContext
{
    public SqliteTestDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Utilizator>().ToTable(tb =>
            tb.HasCheckConstraint("CK_Utilizator_TipUtilizator", "[TipUtilizator] IN ('Client', 'Angajat')"));
    }
}
