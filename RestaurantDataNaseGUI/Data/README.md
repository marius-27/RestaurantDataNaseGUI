# Stratul de date (EF Core) - RestaurantDataNaseGUI

Acest folder contine stratul de acces la date, construit peste schema deja
existenta in `database/schema.sql`. **Nu se folosesc EF Core Migrations.**
Baza de date se creeaza exclusiv ruland scriptul SQL; DbContext-ul doar
mapeaza peste ea (abordare "Database First" fara migrations). Nu apelati
niciodata `Database.Migrate()` sau `Database.EnsureCreated()` — schema si
datele seed sunt responsabilitatea `schema.sql`.

## Fisiere

### `RestaurantDbContext.cs`
`DbContext`-ul principal. Expune un `DbSet<T>` pentru fiecare tabela din
schema (`Categorie`, `Alergen`, `Configurare`, `StareComanda`, `Preparat`,
`PreparatImagine`, `PreparatAlergen`, `Meniu`, `MeniuPreparat`, `Utilizator`,
`Comanda`, `ComandaDetaliu`), plus trei `DbSet<T>` pentru DTO-urile
procedurilor stocate (vezi mai jos).

Toata maparea este facuta prin Fluent API in `OnModelCreating`, intr-o
metoda separata per entitate:
- **Chei compuse**: `PreparatAlergen` → `(PreparatId, AlergenId)`,
  `MeniuPreparat` → `(MeniuId, PreparatId)`.
- **Constrangeri unice**: `Categorie.Denumire`, `Alergen.Denumire`,
  `Configurare.Cheie`, `StareComanda.Denumire`, `Utilizator.Email`,
  `Comanda.CodUnic`.
- **CHECK constraints** mapate 1:1 cu cele din `schema.sql` (via
  `entity.ToTable(tb => tb.HasCheckConstraint(...))`, suportat din EF Core 7+)
  — utile ca metadate/documentare a modelului chiar daca schema fizica este
  creata de script, nu de EF.
- **Reguli de stergere (`OnDelete`)** copiate exact din FK-urile din
  `schema.sql`: `Cascade` acolo unde exista `ON DELETE CASCADE`
  (`PreparatAlergen`, `PreparatImagine`, `MeniuPreparat → Meniu`,
  `ComandaDetaliu → Comanda`), `Restrict` peste tot unde schema nu specifica
  cascadare (`Preparat → Categorie`, `Meniu → Categorie`,
  `MeniuPreparat → Preparat`, `Comanda → Utilizator/StareComanda`,
  `ComandaDetaliu → Preparat/Meniu`). Acest lucru reflecta si conventia de
  soft-delete descrisa in `database/README.md`: un `Preparat`/`Meniu` folosit
  deja intr-o comanda nu poate fi sters, nici prin schema SQL, nici prin EF.
- **Precizie zecimala** (`HasPrecision`) si lungimi de sir (`HasMaxLength`)
  identice cu tipurile `DECIMAL(p,s)` / `NVARCHAR(n)` / `VARCHAR(n)` din
  schema (`Comanda.CodUnic` este mapat explicit `IsUnicode(false)` pentru ca
  in schema e `VARCHAR`, nu `NVARCHAR`).

### `DatabaseConfig.cs`
Construieste `IConfiguration` din `appsettings.json` (+ optional
`appsettings.Development.json`) si, din connection string-ul citit de acolo,
`DbContextOptions<RestaurantDbContext>`. Connection string-ul **nu este
niciodata hardcodat in cod** — se citeste la runtime din configurare, sub
cheia `ConnectionStrings:RestaurantDataNase`. Ofera si `CreateDbContext(...)`
ca metoda rapida de a obtine un `RestaurantDbContext` functional fara a
configura manual un container DI.

### `appsettings.json` (la radacina proiectului)
Contine connection string-ul implicit (`Server=localhost;Database=...`).
Copiat automat in directorul de output la build
(`CopyToOutputDirectory = PreserveNewest`, vezi `.csproj`). Pentru medii
diferite se poate adauga `appsettings.Development.json` (optional, e deja
cautat de `DatabaseConfig`) fara a-l versiona cu credentiale reale.

### `StoredProcedureRepository.cs`
Apeleaza cele 7 proceduri stocate din `schema.sql`:

| Procedura | Metoda | Cum se apeleaza |
|---|---|---|
| `sp_CreateComanda` | `CreateComandaAsync` | ADO.NET direct (`SqlCommand` + `SqlParameter` cu `ParameterDirection.Output`) |
| `sp_AdaugaDetaliuComanda` | `AdaugaDetaliuComandaAsync` | `ExecuteSqlInterpolatedAsync` |
| `sp_UpdateCantitateTotalaLaComanda` | `UpdateCantitateTotalaLaComandaAsync` | `ExecuteSqlInterpolatedAsync` |
| `sp_GetComenziClientCuDetalii` *(complexa)* | `GetComenziClientCuDetaliiAsync` | `FromSqlInterpolated` → `List<ComenziClientDetaliuDto>` |
| `sp_GetPreparateApropiateDeEpuizare` | `GetPreparateApropiateDeEpuizareAsync` | `FromSqlInterpolated` → `List<PreparatEpuizareDto>` |
| `sp_GetMeniuRestaurantCuAlergeni` *(complexa)* | `GetMeniuRestaurantCuAlergeniAsync` | `FromSqlInterpolated` → `List<MeniuCuAlergeniDto>` |
| `sp_SetPreparatIndisponibil` | `SetPreparatIndisponibilAsync` | `ExecuteSqlInterpolatedAsync` |

Toate metodele folosesc parametri **interpolati** (`FromSqlInterpolated` /
`ExecuteSqlInterpolatedAsync`), care EF Core ii transforma automat in
`DbParameter` reali — **niciodata** `FromSqlRaw`/`ExecuteSqlRaw` cu
concatenare de string, ca sa nu existe nicio suprafata de SQL Injection.

Singura exceptie este `sp_CreateComanda`: are doi parametri `OUTPUT`
(`@ComandaId`, `@CodUnic`), pe care `FromSqlInterpolated` nu ii poate
recupera. Pentru acest caz se foloseste ADO.NET direct, prin
`Database.GetDbConnection()` (conexiunea gestionata de `DbContext`), cu
`SqlParameter`-i explicit `ParameterDirection.Output`; valorile parametrilor
de intrare sunt tot valori de parametru `SqlParameter`, nu text concatenat,
deci la fel de sigure.

### `Models/DTOs/`
DTO-uri pentru rezultatele procedurilor ale caror coloane nu corespund 1:1
unei entitati existente: `ComenziClientDetaliuDto`, `PreparatEpuizareDto`,
`MeniuCuAlergeniDto`. Sunt inregistrate in `RestaurantDbContext` ca tipuri
**keyless** (`HasNoKey()`), nemapate pe niciun tabel/view — se populeaza
exclusiv prin `FromSqlInterpolated`, exact ca in exemplul oficial EF Core
pentru rezultate de proceduri stocate.

## De ce fara migrations

Schema (tabele, constrangeri, functie, proceduri) e deja completa si
versionata in `database/schema.sql`, care e si idempotent (isi face drop la
obiectele proprii inainte de a le recrea). A introduce EF Core Migrations
peste asta ar crea doua surse de adevar pentru aceeasi schema. De aceea:
- nu exista folder `Migrations/`;
- `RestaurantDbContext` nu apeleaza `Database.Migrate()` /
  `Database.EnsureCreated()` nicaieri;
- fluxul de configurare a bazei ramane: ruleaza `database/schema.sql` o
  singura data (sau de cate ori e nevoie — e idempotent), apoi porneste
  aplicatia, care doar se conecteaza la baza deja creata.
