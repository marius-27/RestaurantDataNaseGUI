using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RestaurantDataNaseGUI.Data;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

Console.WriteLine("=== Populare date de test/demo - RestaurantDataNaseGUI ===");

var configuration = DatabaseConfig.BuildConfiguration();
var connectionString = DatabaseConfig.GetConnectionString(configuration);
Console.WriteLine($"Connection string folosit: {MaskeazaParola(connectionString)}");
Console.WriteLine();

var adminService = new AdminService();
var authService = new AuthService();
var orderService = new OrderService();

// 1) Angajat - inserat direct prin DbContext, TipUtilizator="Angajat" nu are
//    un flux de inregistrare propriu in AuthService/AdminService.
var angajat = await SeedAngajatAsync();
SessionService.Instance.SetCurrentUser(angajat);

// 2) Categorii + Alergeni (necesita sesiune de angajat pentru AdminService).
var categorii = await SeedCategoriiAsync(adminService);
var alergeni = await SeedAlergeniAsync(adminService);

// 3) Preparate + Meniuri, in aceasta ordine (meniurile refera preparate deja create).
var preparate = await SeedPreparateAsync(adminService, categorii, alergeni);
var meniuri = await SeedMeniuriAsync(adminService, categorii, preparate);
var preturiMeniuri = await GetPreturiMeniuriAsync();

// 4) Clienti - prin AuthService.RegisterAsync, ca sa respecte fluxul real de inregistrare.
var client1 = await SeedClientAsync(
    authService, nume: "Popescu", prenume: "Andrei", email: "client1@test.ro",
    telefon: "0722111222", adresa: "Str. Lalelelor nr. 5, Brasov", parola: "Client123!");
var client2 = await SeedClientAsync(
    authService, nume: "Georgescu", prenume: "Maria", email: "client2@test.ro",
    telefon: "0733222333", adresa: "Str. Trandafirilor nr. 10, Brasov", parola: "Client123!");

// 5) Cateva comenzi de test pentru client1, in stari diferite.
await SeedComenziAsync(orderService, angajat, client1, preparate, meniuri, preturiMeniuri);

SessionService.Instance.Logout();

Console.WriteLine();
Console.WriteLine("=== Populare finalizata cu succes. ===");

// ----------------------------------------------------------------------
// Angajat
// ----------------------------------------------------------------------

static async Task<Utilizator> SeedAngajatAsync()
{
    const string email = "angajat@restaurant.ro";

    await using var context = DatabaseConfig.CreateDbContext();

    var existent = await context.Utilizatori.FirstOrDefaultAsync(u => u.Email == email);
    if (existent is not null)
    {
        Console.WriteLine($"Angajat '{email}' exista deja - se omite.");
        return existent;
    }

    var angajat = new Utilizator
    {
        Nume = "Ionescu",
        Prenume = "Maria",
        Email = email,
        Telefon = "0722000111",
        AdresaLivrare = null,
        ParolaHash = BCrypt.Net.BCrypt.HashPassword("Angajat123!"),
        TipUtilizator = "Angajat",
    };

    context.Utilizatori.Add(angajat);
    await context.SaveChangesAsync();

    Console.WriteLine($"Angajat '{email}' creat.");
    return angajat;
}

// ----------------------------------------------------------------------
// Categorii / Alergeni
// ----------------------------------------------------------------------

static async Task<Dictionary<string, Categorie>> SeedCategoriiAsync(AdminService adminService)
{
    string[] denumiri = { "Mic dejun", "Aperitive", "Supe si ciorbe", "Fel principal", "Deserturi", "Bauturi" };

    var existente = (await adminService.GetCategoriiAsync()).Select(c => c.Denumire).ToHashSet(StringComparer.Ordinal);

    foreach (var denumire in denumiri)
    {
        if (existente.Contains(denumire))
        {
            Console.WriteLine($"Categorie '{denumire}' exista deja - se omite.");
            continue;
        }

        var rezultat = await adminService.CreeazaCategorieAsync(new CategorieFormDto { Denumire = denumire });
        if (!rezultat.Succes)
        {
            throw new InvalidOperationException($"Nu s-a putut crea categoria '{denumire}': {rezultat.MesajEroare}");
        }

        Console.WriteLine($"Categorie '{denumire}' creata.");
    }

    return (await adminService.GetCategoriiAsync()).ToDictionary(c => c.Denumire, StringComparer.Ordinal);
}

static async Task<Dictionary<string, Alergen>> SeedAlergeniAsync(AdminService adminService)
{
    string[] denumiri = { "Gluten", "Oua", "Lactoza", "Telina", "Peste", "Fructe de mare", "Soia", "Arahide" };

    var existente = (await adminService.GetAlergeniAsync()).Select(a => a.Denumire).ToHashSet(StringComparer.Ordinal);

    foreach (var denumire in denumiri)
    {
        if (existente.Contains(denumire))
        {
            Console.WriteLine($"Alergen '{denumire}' exista deja - se omite.");
            continue;
        }

        var rezultat = await adminService.CreeazaAlergenAsync(new AlergenFormDto { Denumire = denumire });
        if (!rezultat.Succes)
        {
            throw new InvalidOperationException($"Nu s-a putut crea alergenul '{denumire}': {rezultat.MesajEroare}");
        }

        Console.WriteLine($"Alergen '{denumire}' creat.");
    }

    return (await adminService.GetAlergeniAsync()).ToDictionary(a => a.Denumire, StringComparer.Ordinal);
}

// ----------------------------------------------------------------------
// Clienti
// ----------------------------------------------------------------------

static async Task<Utilizator> SeedClientAsync(
    AuthService authService, string nume, string prenume, string email, string telefon, string adresa, string parola)
{
    await using var context = DatabaseConfig.CreateDbContext();

    var existent = await context.Utilizatori.FirstOrDefaultAsync(u => u.Email == email);
    if (existent is not null)
    {
        Console.WriteLine($"Client '{email}' exista deja - se omite.");
        return existent;
    }

    var rezultat = await authService.RegisterAsync(nume, prenume, email, telefon, adresa, parola);
    if (!rezultat.Succes)
    {
        throw new InvalidOperationException($"Nu s-a putut inregistra clientul '{email}': {rezultat.MesajEroare}");
    }

    Console.WriteLine($"Client '{email}' creat.");
    return rezultat.Utilizator!;
}

// ----------------------------------------------------------------------
// Preparate
// ----------------------------------------------------------------------

static async Task<Dictionary<string, Preparat>> SeedPreparateAsync(
    AdminService adminService, Dictionary<string, Categorie> categorii, Dictionary<string, Alergen> alergeni)
{
    var seed = new List<PreparatSeed>
    {
        // Mic dejun
        new("Omleta cu cascaval", "Mic dejun", 18m, 250m, "g", 3000m, true, new[] { "Oua", "Lactoza" }, "omleta-cu-cascaval.jpg"),
        new("Clatite cu dulceata", "Mic dejun", 16m, 200m, "g", 2500m, true, new[] { "Gluten", "Oua", "Lactoza" }, "clatite-cu-dulceata.jpg"),
        new("Sandvis cu sunca si cascaval", "Mic dejun", 15m, 220m, "g", 2800m, true, new[] { "Gluten", "Lactoza" }, "sandvis-cu-sunca-si-cascaval.jpg"),

        // Aperitive
        new("Salata de vinete", "Aperitive", 17m, 250m, "g", 3200m, true, Array.Empty<string>(), "salata-de-vinete.jpg"),
        new("Bruschete cu rosii si busuioc", "Aperitive", 19m, 220m, "g", 2600m, true, new[] { "Gluten" }, "bruschete-cu-rosii-si-busuioc.jpg"),
        new("Chiftelute de post", "Aperitive", 20m, 250m, "g", 8m, true, new[] { "Gluten", "Soia" }, "chiftelute-de-post.jpg"), // sub pragul de epuizare (10)

        // Supe si ciorbe
        new("Ciorba de burta", "Supe si ciorbe", 22m, 350m, "ml", 4000m, true, new[] { "Gluten", "Oua" }, "ciorba-de-burta.jpg"),
        new("Supa crema de ciuperci", "Supe si ciorbe", 20m, 300m, "ml", 3500m, true, new[] { "Lactoza" }, "supa-crema-de-ciuperci.jpg"),
        new("Ciorba radauteana", "Supe si ciorbe", 23m, 350m, "ml", 5m, true, new[] { "Oua", "Lactoza" }, "ciorba-radauteana.jpg"), // sub pragul de epuizare

        // Fel principal
        new("Sarmale cu mamaliga", "Fel principal", 35m, 400m, "g", 6000m, true, Array.Empty<string>(), "sarmale-cu-mamaliga.png"),
        new("Piept de pui la gratar cu legume", "Fel principal", 32m, 350m, "g", 5000m, true, Array.Empty<string>(), "piept-de-pui-la-gratar-cu-legume.jpg"),
        new("Paste carbonara", "Fel principal", 28m, 300m, "g", 4200m, true, new[] { "Gluten", "Oua", "Lactoza" }, "paste-carbonara.jpg"),
        new("Pizza Margherita", "Fel principal", 30m, 400m, "g", 4500m, true, new[] { "Gluten", "Lactoza" }, "pizza-margherita.jpg"),
        new("Somon la cuptor cu legume", "Fel principal", 45m, 300m, "g", 2200m, true, new[] { "Peste" }, "somon-la-cuptor-cu-legume.jpg"),
        new("Mici cu mustar", "Fel principal", 25m, 250m, "g", 3000m, false, Array.Empty<string>(), "mici-cu-mustar.jpg"), // indisponibil

        // Deserturi
        new("Tiramisu", "Deserturi", 22m, 200m, "g", 2000m, true, new[] { "Gluten", "Oua", "Lactoza" }, "tiramisu.jpg"),
        new("Papanasi cu smantana si dulceata", "Deserturi", 24m, 300m, "g", 2500m, true, new[] { "Gluten", "Oua", "Lactoza" }, "papanasi-cu-smantana-si-dulceata.jpg"),
        new("Tarta cu fructe de padure", "Deserturi", 26m, 200m, "g", 1800m, false, new[] { "Gluten", "Oua", "Lactoza" }, "tarta-cu-fructe-de-padure.jpg"), // indisponibil

        // Bauturi
        new("Limonada de casa", "Bauturi", 15m, 400m, "ml", 6000m, true, Array.Empty<string>(), "limonada-de-casa.jpg"),
        new("Suc de portocale natural", "Bauturi", 16m, 300m, "ml", 5500m, true, Array.Empty<string>(), "suc-de-portocale-natural.jpg"),
        new("Cafea espresso", "Bauturi", 15m, 200m, "ml", 9m, true, Array.Empty<string>(), "cafea-espresso.jpg"), // sub pragul de epuizare
    };

    var imaginiDir = GetImaginiDirectory();
    var existente = (await adminService.GetPreparateAsync()).ToDictionary(p => p.Denumire, StringComparer.Ordinal);

    foreach (var item in seed)
    {
        var caleImagine = Path.Combine(imaginiDir, item.ImagineFile);

        if (existente.TryGetValue(item.Denumire, out var preparatExistent))
        {
            if (preparatExistent.Imagini.Count > 0)
            {
                Console.WriteLine($"Preparat '{item.Denumire}' exista deja - se omite.");
                continue;
            }

            // A fost creat de o rulare anterioara a scriptului, inainte sa aiba
            // imagini asociate - completam doar imaginea, restul campurilor raman neschimbate.
            var formActualizare = new PreparatFormDto
            {
                Id = preparatExistent.Id,
                Denumire = preparatExistent.Denumire,
                Pret = preparatExistent.Pret,
                CantitatePortie = preparatExistent.CantitatePortie,
                UnitateMasura = preparatExistent.UnitateMasura,
                CantitateTotalaRestaurant = preparatExistent.CantitateTotalaRestaurant,
                CategorieId = preparatExistent.CategorieId,
                Disponibil = preparatExistent.Disponibil,
                AlergenIds = preparatExistent.PreparatAlergeni.Select(pa => pa.AlergenId).ToList(),
                ImaginiPaths = new List<string> { caleImagine },
            };

            var rezultatActualizare = await adminService.ActualizeazaPreparatAsync(formActualizare);
            if (!rezultatActualizare.Succes)
            {
                throw new InvalidOperationException($"Nu s-a putut adauga imaginea la preparatul '{item.Denumire}': {rezultatActualizare.MesajEroare}");
            }

            Console.WriteLine($"Preparat '{item.Denumire}' exista deja - imagine adaugata.");
            continue;
        }

        var form = new PreparatFormDto
        {
            Denumire = item.Denumire,
            Pret = item.Pret,
            CantitatePortie = item.CantitatePortie,
            UnitateMasura = item.UnitateMasura,
            CantitateTotalaRestaurant = item.Stoc,
            CategorieId = categorii[item.Categorie].Id,
            Disponibil = item.Disponibil,
            AlergenIds = item.Alergeni.Select(a => alergeni[a].Id).ToList(),
            ImaginiPaths = new List<string> { caleImagine },
        };

        var rezultat = await adminService.CreeazaPreparatAsync(form);
        if (!rezultat.Succes)
        {
            throw new InvalidOperationException($"Nu s-a putut crea preparatul '{item.Denumire}': {rezultat.MesajEroare}");
        }

        Console.WriteLine($"Preparat '{item.Denumire}' creat.");
    }

    return (await adminService.GetPreparateAsync()).ToDictionary(p => p.Denumire, StringComparer.Ordinal);
}

/// <summary>
/// Locatia folderului cu poze demo (tools/SeedData/Images) - incearca intai
/// directorul curent (cazul uzual, "dotnet run" din tools/SeedData), apoi
/// langa executabil (fallback dupa build/publish).
/// </summary>
static string GetImaginiDirectory()
{
    string[] candidati =
    {
        Path.Combine(Directory.GetCurrentDirectory(), "Images"),
        Path.Combine(AppContext.BaseDirectory, "Images"),
    };

    foreach (var candidat in candidati)
    {
        if (Directory.Exists(candidat))
        {
            return candidat;
        }
    }

    throw new InvalidOperationException(
        "Nu s-a gasit folderul cu poze (tools/SeedData/Images). Ruleaza 'dotnet run' din folderul tools/SeedData.");
}

// ----------------------------------------------------------------------
// Meniuri
// ----------------------------------------------------------------------

static async Task<Dictionary<string, Meniu>> SeedMeniuriAsync(
    AdminService adminService, Dictionary<string, Categorie> categorii, Dictionary<string, Preparat> preparate)
{
    var seed = new List<MeniuSeed>
    {
        new("Meniu Traditional", "Fel principal", new[]
        {
            new MeniuComponentaSeed("Ciorba de burta", 300m),
            new MeniuComponentaSeed("Sarmale cu mamaliga", 350m),
            new MeniuComponentaSeed("Limonada de casa", 330m),
        }),
        new("Meniu Business", "Fel principal", new[]
        {
            new MeniuComponentaSeed("Piept de pui la gratar cu legume", 300m),
            new MeniuComponentaSeed("Salata de vinete", 150m),
            new MeniuComponentaSeed("Suc de portocale natural", 250m),
        }),
        new("Meniu Pasta cu Desert", "Fel principal", new[]
        {
            new MeniuComponentaSeed("Paste carbonara", 280m),
            new MeniuComponentaSeed("Tiramisu", 150m),
        }),
        new("Mic Dejun Complet", "Mic dejun", new[]
        {
            new MeniuComponentaSeed("Omleta cu cascaval", 200m),
            new MeniuComponentaSeed("Sandvis cu sunca si cascaval", 180m),
            new MeniuComponentaSeed("Cafea espresso", 150m),
        }),
    };

    var existente = (await adminService.GetMeniuriAsync()).Select(m => m.Denumire).ToHashSet(StringComparer.Ordinal);

    foreach (var item in seed)
    {
        if (existente.Contains(item.Denumire))
        {
            Console.WriteLine($"Meniu '{item.Denumire}' exista deja - se omite.");
            continue;
        }

        var form = new MeniuFormDto
        {
            Denumire = item.Denumire,
            CategorieId = categorii[item.Categorie].Id,
            Preparate = item.Componente
                .Select(c => new MeniuPreparatFormDto { PreparatId = preparate[c.Preparat].Id, CantitateInMeniu = c.Cantitate })
                .ToList(),
        };

        var rezultat = await adminService.CreeazaMeniuAsync(form);
        if (!rezultat.Succes)
        {
            throw new InvalidOperationException($"Nu s-a putut crea meniul '{item.Denumire}': {rezultat.MesajEroare}");
        }

        Console.WriteLine($"Meniu '{item.Denumire}' creat.");
    }

    return (await adminService.GetMeniuriAsync()).ToDictionary(m => m.Denumire, StringComparer.Ordinal);
}

static async Task<Dictionary<string, decimal>> GetPreturiMeniuriAsync()
{
    await using var context = DatabaseConfig.CreateDbContext();
    var repository = new StoredProcedureRepository(context);
    var randuri = await repository.GetMeniuRestaurantCuAlergeniAsync();
    return randuri.ToDictionary(r => r.Meniu, r => r.PretCalculat, StringComparer.Ordinal);
}

// ----------------------------------------------------------------------
// Comenzi de test (client1)
// ----------------------------------------------------------------------

static async Task SeedComenziAsync(
    OrderService orderService,
    Utilizator angajat,
    Utilizator client1,
    Dictionary<string, Preparat> preparate,
    Dictionary<string, Meniu> meniuri,
    Dictionary<string, decimal> preturiMeniuri)
{
    var comenziExistente = await orderService.GetComenziClientAsync(client1.Id);
    if (comenziExistente.Count > 0)
    {
        Console.WriteLine($"Clientul '{client1.Email}' are deja {comenziExistente.Count} comenzi - se omite generarea comenzilor de test.");
        return;
    }

    SessionService.Instance.SetCurrentUser(client1);

    var comandaLivrata = await PlaseazaComandaAsync(orderService, client1, new List<ArticolCosDto>
    {
        ArticolPreparat(preparate["Sarmale cu mamaliga"], 2m),
        ArticolPreparat(preparate["Limonada de casa"], 2m),
    });

    _ = await PlaseazaComandaAsync(orderService, client1, new List<ArticolCosDto>
    {
        ArticolPreparat(preparate["Pizza Margherita"], 1m),
        ArticolPreparat(preparate["Tiramisu"], 2m),
    });

    var comandaInPregatire = await PlaseazaComandaAsync(orderService, client1, new List<ArticolCosDto>
    {
        ArticolMeniu(meniuri["Meniu Traditional"], preturiMeniuri["Meniu Traditional"], 1m),
        ArticolPreparat(preparate["Suc de portocale natural"], 2m),
    });

    SessionService.Instance.SetCurrentUser(angajat);

    await AvanseazaStareAsync(orderService, comandaLivrata, "se pregateste");
    await AvanseazaStareAsync(orderService, comandaLivrata, "a plecat la client");
    await AvanseazaStareAsync(orderService, comandaLivrata, "livrata");

    await AvanseazaStareAsync(orderService, comandaInPregatire, "se pregateste");

    Console.WriteLine("Comenzi de test create pentru client1 (stari: livrata / inregistrata / se pregateste).");
}

static ArticolCosDto ArticolPreparat(Preparat preparat, decimal cantitate) => new()
{
    PreparatId = preparat.Id,
    Denumire = preparat.Denumire,
    PretUnitar = preparat.Pret,
    Cantitate = cantitate,
};

static ArticolCosDto ArticolMeniu(Meniu meniu, decimal pretCalculat, decimal cantitate) => new()
{
    MeniuId = meniu.Id,
    Denumire = meniu.Denumire,
    PretUnitar = pretCalculat,
    Cantitate = cantitate,
};

static async Task<int> PlaseazaComandaAsync(OrderService orderService, Utilizator client, List<ArticolCosDto> articole)
{
    var rezultat = await orderService.CreeazaComandaAsync(articole, client.Id);
    if (!rezultat.Succes)
    {
        throw new InvalidOperationException($"Nu s-a putut crea comanda de test pentru '{client.Email}': {rezultat.MesajEroare}");
    }

    Console.WriteLine($"Comanda {rezultat.CodUnic} creata pentru {client.Email}.");
    return rezultat.ComandaId!.Value;
}

static async Task AvanseazaStareAsync(OrderService orderService, int comandaId, string stareNoua)
{
    var rezultat = await orderService.SchimbaStareComandaAsync(comandaId, stareNoua);
    if (!rezultat.Succes)
    {
        throw new InvalidOperationException($"Nu s-a putut schimba starea comenzii {comandaId} in '{stareNoua}': {rezultat.MesajEroare}");
    }

    Console.WriteLine($"Comanda {comandaId} -> '{stareNoua}'.");
}

static string MaskeazaParola(string connectionString) =>
    Regex.Replace(connectionString, @"(Password|Pwd)\s*=\s*[^;]*", "$1=***", RegexOptions.IgnoreCase);

internal sealed record PreparatSeed(
    string Denumire, string Categorie, decimal Pret, decimal CantitatePortie,
    string UnitateMasura, decimal Stoc, bool Disponibil, string[] Alergeni, string ImagineFile);

internal sealed record MeniuComponentaSeed(string Preparat, decimal Cantitate);

internal sealed record MeniuSeed(string Denumire, string Categorie, MeniuComponentaSeed[] Componente);
