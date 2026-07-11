using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.test.TestSupport;

// Subclasa RestaurantDbContext, doar pentru testele SQLite in-memory. CK_Utilizator_TipUtilizator foloseste in
// RestaurantDbContext sintaxa T-SQL "N'Client'" (literal Unicode), pe care SQLite nu o recunoaste, iar EnsureCreated() esueaza.
// Redeclaram acelasi CHECK cu sintaxa portabila (fara N) - echivalent functional pentru cele doua valori ASCII.
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
