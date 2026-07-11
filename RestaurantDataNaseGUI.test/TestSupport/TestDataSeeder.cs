using System.Globalization;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;

namespace RestaurantDataNaseGUI.test.TestSupport;

// Seed pentru datele "lookup" (StareComanda, Configurare) pe care schema.sql le populeaza automat in productie.
// Testele unitare ruleaza pe SQLite goala (SqliteInMemoryDbContextFactory), deci recreeaza acelasi seed minim pentru OrderService.
public static class TestDataSeeder
{
    // Cele 5 stari standard, identice cu schema.sql.
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

    // Cheile din dbo.Configurare folosite de OrderService.CalculeazaCostAsync, cu valorile implicite din schema.sql -
    // override-uieste orice parametru cand testul are nevoie de alta valoare.
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
