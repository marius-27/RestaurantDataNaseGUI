using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Data;

/// <summary>
/// DbContext care mapeaza pe schema deja existenta in database/schema.sql
/// ("Database First" fara migrations). Baza de date trebuie creata rulind
/// acel script - acest DbContext NU o creeaza si NU trebuie folosit niciodata
/// cu Database.Migrate() sau Database.EnsureCreated().
/// </summary>
public class RestaurantDbContext : DbContext
{
    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Categorie> Categorii => Set<Categorie>();
    public DbSet<Alergen> Alergeni => Set<Alergen>();
    public DbSet<Configurare> Configurari => Set<Configurare>();
    public DbSet<StareComanda> StariComanda => Set<StareComanda>();
    public DbSet<Preparat> Preparate => Set<Preparat>();
    public DbSet<PreparatImagine> PreparatImagini => Set<PreparatImagine>();
    public DbSet<PreparatAlergen> PreparatAlergeni => Set<PreparatAlergen>();
    public DbSet<Meniu> Meniuri => Set<Meniu>();
    public DbSet<MeniuPreparat> MeniuPreparate => Set<MeniuPreparat>();
    public DbSet<Utilizator> Utilizatori => Set<Utilizator>();
    public DbSet<Comanda> Comenzi => Set<Comanda>();
    public DbSet<ComandaDetaliu> ComandaDetalii => Set<ComandaDetaliu>();

    // DTO-uri "keyless" - populate exclusiv prin FromSqlInterpolated in
    // StoredProcedureRepository, nu corespund unui tabel din schema.
    public DbSet<ComenziClientDetaliuDto> ComenziClientDetalii => Set<ComenziClientDetaliuDto>();
    public DbSet<PreparatEpuizareDto> PreparateApropiateDeEpuizare => Set<PreparatEpuizareDto>();
    public DbSet<MeniuCuAlergeniDto> MeniuriCuAlergeni => Set<MeniuCuAlergeniDto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureCategorie(modelBuilder);
        ConfigureAlergen(modelBuilder);
        ConfigureConfigurare(modelBuilder);
        ConfigureStareComanda(modelBuilder);
        ConfigurePreparat(modelBuilder);
        ConfigurePreparatImagine(modelBuilder);
        ConfigurePreparatAlergen(modelBuilder);
        ConfigureMeniu(modelBuilder);
        ConfigureMeniuPreparat(modelBuilder);
        ConfigureUtilizator(modelBuilder);
        ConfigureComanda(modelBuilder);
        ConfigureComandaDetaliu(modelBuilder);
        ConfigureDtos(modelBuilder);
    }

    private static void ConfigureCategorie(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categorie>(entity =>
        {
            entity.ToTable("Categorie");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Denumire).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Denumire).IsUnique();
        });
    }

    private static void ConfigureAlergen(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alergen>(entity =>
        {
            entity.ToTable("Alergen");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Denumire).IsRequired().HasMaxLength(100);
            entity.HasIndex(a => a.Denumire).IsUnique();
        });
    }

    private static void ConfigureConfigurare(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Configurare>(entity =>
        {
            entity.ToTable("Configurare");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Cheie).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Valoare).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Descriere).HasMaxLength(255);
            entity.HasIndex(c => c.Cheie).IsUnique();
        });
    }

    private static void ConfigureStareComanda(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StareComanda>(entity =>
        {
            entity.ToTable("StareComanda");
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Denumire).IsRequired().HasMaxLength(50);
            entity.HasIndex(s => s.Denumire).IsUnique();
        });
    }

    private static void ConfigurePreparat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Preparat>(entity =>
        {
            entity.ToTable("Preparat", tb =>
            {
                tb.HasCheckConstraint("CK_Preparat_Pret", "[Pret] > 0");
                tb.HasCheckConstraint("CK_Preparat_CantitatePortie", "[CantitatePortie] > 0");
                tb.HasCheckConstraint("CK_Preparat_CantitateTotalaRestaurant", "[CantitateTotalaRestaurant] >= 0");
            });
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Denumire).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Pret).HasPrecision(10, 2);
            entity.Property(p => p.CantitatePortie).HasPrecision(10, 2);
            entity.Property(p => p.UnitateMasura).IsRequired().HasMaxLength(20);
            entity.Property(p => p.CantitateTotalaRestaurant).HasPrecision(10, 2);
            entity.Property(p => p.Disponibil).HasDefaultValue(true);

            entity.HasOne(p => p.Categorie)
                .WithMany(c => c.Preparate)
                .HasForeignKey(p => p.CategorieId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.CategorieId);
        });
    }

    private static void ConfigurePreparatImagine(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PreparatImagine>(entity =>
        {
            entity.ToTable("PreparatImagine");
            entity.HasKey(pi => pi.Id);
            entity.Property(pi => pi.CalePoza).IsRequired().HasMaxLength(500);

            entity.HasOne(pi => pi.Preparat)
                .WithMany(p => p.Imagini)
                .HasForeignKey(pi => pi.PreparatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pi => pi.PreparatId);
        });
    }

    private static void ConfigurePreparatAlergen(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PreparatAlergen>(entity =>
        {
            entity.ToTable("PreparatAlergen");
            entity.HasKey(pa => new { pa.PreparatId, pa.AlergenId });

            entity.HasOne(pa => pa.Preparat)
                .WithMany(p => p.PreparatAlergeni)
                .HasForeignKey(pa => pa.PreparatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pa => pa.Alergen)
                .WithMany(a => a.PreparatAlergeni)
                .HasForeignKey(pa => pa.AlergenId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pa => pa.AlergenId);
        });
    }

    private static void ConfigureMeniu(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Meniu>(entity =>
        {
            entity.ToTable("Meniu");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Denumire).IsRequired().HasMaxLength(150);

            entity.HasOne(m => m.Categorie)
                .WithMany(c => c.Meniuri)
                .HasForeignKey(m => m.CategorieId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.CategorieId);

            // Pretul NU e coloana - se calculeaza dinamic in baza de date
            // prin dbo.fn_CalculeazaPretMeniu, deci nu exista proprietate/
            // mapare pentru el pe aceasta entitate.
        });
    }

    private static void ConfigureMeniuPreparat(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeniuPreparat>(entity =>
        {
            entity.ToTable("MeniuPreparat", tb =>
            {
                tb.HasCheckConstraint("CK_MeniuPreparat_Cantitate", "[CantitateInMeniu] > 0");
            });
            entity.HasKey(mp => new { mp.MeniuId, mp.PreparatId });
            entity.Property(mp => mp.CantitateInMeniu).HasPrecision(10, 2);

            entity.HasOne(mp => mp.Meniu)
                .WithMany(m => m.MeniuPreparate)
                .HasForeignKey(mp => mp.MeniuId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK_MeniuPreparat_Preparat este ON DELETE NO ACTION in schema.sql
            entity.HasOne(mp => mp.Preparat)
                .WithMany(p => p.MeniuPreparate)
                .HasForeignKey(mp => mp.PreparatId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(mp => mp.PreparatId);
        });
    }

    private static void ConfigureUtilizator(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Utilizator>(entity =>
        {
            entity.ToTable("Utilizator", tb =>
            {
                tb.HasCheckConstraint("CK_Utilizator_TipUtilizator", "[TipUtilizator] IN (N'Client', N'Angajat')");
                tb.HasCheckConstraint("CK_Utilizator_Email", "[Email] LIKE '%_@__%.__%'");
            });
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Nume).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Prenume).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.Property(u => u.Telefon).IsRequired().HasMaxLength(20);
            entity.Property(u => u.AdresaLivrare).HasMaxLength(300);
            entity.Property(u => u.ParolaHash).IsRequired().HasMaxLength(255);
            entity.Property(u => u.TipUtilizator).IsRequired().HasMaxLength(20);

            entity.HasIndex(u => u.Email).IsUnique();
        });
    }

    private static void ConfigureComanda(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Comanda>(entity =>
        {
            entity.ToTable("Comanda", tb =>
            {
                tb.HasCheckConstraint("CK_Comanda_CostTransport", "[CostTransport] >= 0");
                tb.HasCheckConstraint("CK_Comanda_Discount", "[Discount] >= 0 AND [Discount] <= 100");
            });
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CodUnic).IsRequired().HasMaxLength(20).IsUnicode(false);
            entity.Property(c => c.DataComanda).IsRequired();
            entity.Property(c => c.CostTransport).HasPrecision(10, 2);
            entity.Property(c => c.Discount).HasPrecision(5, 2);

            entity.HasIndex(c => c.CodUnic).IsUnique();

            entity.HasOne(c => c.Utilizator)
                .WithMany(u => u.Comenzi)
                .HasForeignKey(c => c.UtilizatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Stare)
                .WithMany(s => s.Comenzi)
                .HasForeignKey(c => c.StareId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.UtilizatorId);
            entity.HasIndex(c => c.StareId);
        });
    }

    private static void ConfigureComandaDetaliu(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComandaDetaliu>(entity =>
        {
            entity.ToTable("ComandaDetaliu", tb =>
            {
                tb.HasCheckConstraint("CK_ComandaDetaliu_Cantitate", "[Cantitate] > 0");
                tb.HasCheckConstraint("CK_ComandaDetaliu_PretUnitar", "[PretUnitarLaComanda] > 0");
                tb.HasCheckConstraint(
                    "CK_ComandaDetaliu_PreparatSauMeniu",
                    "([PreparatId] IS NOT NULL AND [MeniuId] IS NULL) OR ([PreparatId] IS NULL AND [MeniuId] IS NOT NULL)");
            });
            entity.HasKey(cd => cd.Id);
            entity.Property(cd => cd.Cantitate).HasPrecision(10, 2);
            entity.Property(cd => cd.PretUnitarLaComanda).HasPrecision(10, 2);

            entity.HasOne(cd => cd.Comanda)
                .WithMany(c => c.ComandaDetalii)
                .HasForeignKey(cd => cd.ComandaId)
                .OnDelete(DeleteBehavior.Cascade);

            // FK_ComandaDetaliu_Preparat / FK_ComandaDetaliu_Meniu sunt fara
            // ON DELETE CASCADE in schema.sql - un preparat/meniu folosit
            // intr-o comanda nu poate fi sters (vezi conventia de soft-delete
            // din database/README.md).
            entity.HasOne(cd => cd.Preparat)
                .WithMany(p => p.ComandaDetalii)
                .HasForeignKey(cd => cd.PreparatId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(cd => cd.Meniu)
                .WithMany(m => m.ComandaDetalii)
                .HasForeignKey(cd => cd.MeniuId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(cd => cd.ComandaId);
            entity.HasIndex(cd => cd.PreparatId);
            entity.HasIndex(cd => cd.MeniuId);
        });
    }

    private static void ConfigureDtos(ModelBuilder modelBuilder)
    {
        // Tipuri "keyless", nu sunt mapate pe niciun tabel/view - se
        // populeaza exclusiv prin FromSqlInterpolated in StoredProcedureRepository.
        modelBuilder.Entity<ComenziClientDetaliuDto>().HasNoKey();
        modelBuilder.Entity<PreparatEpuizareDto>().HasNoKey();
        modelBuilder.Entity<MeniuCuAlergeniDto>().HasNoKey();
    }
}
