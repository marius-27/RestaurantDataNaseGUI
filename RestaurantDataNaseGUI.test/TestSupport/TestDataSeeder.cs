using System.Globalization;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.test.TestSupport;

/// <summary>
/// Seed pentru datele "lookup" pe care database/schema.sql le populeaza
/// automat intr-o baza de date reala (StareComanda, Configurare) - vezi
/// sectiunea "Date seed incluse" din database/README.md. Testele unitare
/// ruleaza pe o baza SQLite goala (<see cref="SqliteInMemoryDbContextFactory"/>),
/// deci trebuie sa recreeze acelasi seed minim ca sa poata exercita logica
/// din OrderService care citeste din aceste tabele.
/// </summary>
public static class TestDataSeeder
{
    /// <summary>Cele 5 stari standard, identice cu schema.sql.</summary>
    public static async Task SeedStariComandaAsync(RestaurantDbContext context)
    {
        context.StariComanda.AddRange(
            new StareComanda { Denumire = "inregistrata" },
            new StareComanda { Denumire = "se pregateste" },
            new StareComanda { Denumire = "a plecat la client" },
            new StareComanda { Denumire = "livrata" },
            new StareComanda { Denumire = "anulata" });

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Cheile din dbo.Configurare folosite de OrderService.CalculeazaCostAsync,
    /// cu aceleasi valori implicite ca seed-ul din database/schema.sql -
    /// override-uieste orice parametru cand un test are nevoie de o alta
    /// valoare (ex. praguri diferite).
    /// </summary>
    public static async Task SeedConfigurareComandaAsync(
        RestaurantDbContext context,
        decimal sumaMinimaComandaDiscount = 100,
        decimal numarComenziPentruDiscount = 5,
        decimal intervalTimpDiscount = 30,
        decimal procentDiscountFrecventa = 5,
        decimal pragTransportGratuit = 150,
        decimal costTransport = 15)
    {
        context.Configurari.AddRange(
            new Configurare { Cheie = "SumaMinimaComandaDiscount", Valoare = sumaMinimaComandaDiscount.ToString(CultureInfo.InvariantCulture) },
            new Configurare { Cheie = "NumarComenziPentruDiscount", Valoare = numarComenziPentruDiscount.ToString(CultureInfo.InvariantCulture) },
            new Configurare { Cheie = "IntervalTimpDiscount", Valoare = intervalTimpDiscount.ToString(CultureInfo.InvariantCulture) },
            new Configurare { Cheie = "ProcentDiscountFrecventa", Valoare = procentDiscountFrecventa.ToString(CultureInfo.InvariantCulture) },
            new Configurare { Cheie = "PragTransportGratuit", Valoare = pragTransportGratuit.ToString(CultureInfo.InvariantCulture) },
            new Configurare { Cheie = "CostTransport", Valoare = costTransport.ToString(CultureInfo.InvariantCulture) });

        await context.SaveChangesAsync();
    }
}
