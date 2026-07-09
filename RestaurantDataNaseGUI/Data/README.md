# Stratul de date (EF Core) - RestaurantDataNaseGUI

Acest folder conține stratul de acces la date, construit peste schema deja
existentă în `database/schema.sql`. **Nu se folosesc EF Core Migrations.**
Baza de date se creează exclusiv rulând scriptul SQL; DbContext-ul doar
mapează peste ea (abordare "Database First" fără migrations). Nu apelați
niciodată `Database.Migrate()` sau `Database.EnsureCreated()` — schema și
datele seed sunt responsabilitatea `schema.sql`.

## Fișiere

### `RestaurantDbContext.cs`
`DbContext`-ul principal. Expune un `DbSet<T>` pentru fiecare tabelă din
schemă (`Categorie`, `Alergen`, `Configurare`, `StareComanda`, `Preparat`,
`PreparatImagine`, `PreparatAlergen`, `Meniu`, `MeniuPreparat`, `Utilizator`,
`Comanda`, `ComandaDetaliu`), plus trei `DbSet<T>` pentru DTO-urile
procedurilor stocate (vezi mai jos).

Toată maparea este făcută prin Fluent API în `OnModelCreating`, într-o
metodă separată per entitate:
- **Chei compuse**: `PreparatAlergen` → `(PreparatId, AlergenId)`,
  `MeniuPreparat` → `(MeniuId, PreparatId)`.
- **Constrângeri unice**: `Categorie.Denumire`, `Alergen.Denumire`,
  `Configurare.Cheie`, `StareComanda.Denumire`, `Utilizator.Email`,
  `Comanda.CodUnic`.
- **CHECK constraints** mapate 1:1 cu cele din `schema.sql` (via
  `entity.ToTable(tb => tb.HasCheckConstraint(...))`, suportat din EF Core 7+)
  — utile ca metadate/documentare a modelului chiar dacă schema fizică este
  creată de script, nu de EF.
- **Reguli de ștergere (`OnDelete`)** copiate exact din FK-urile din
  `schema.sql`: `Cascade` acolo unde există `ON DELETE CASCADE`
  (`PreparatAlergen`, `PreparatImagine`, `MeniuPreparat → Meniu`,
  `ComandaDetaliu → Comanda`), `Restrict` peste tot unde schema nu specifică
  cascadare (`Preparat → Categorie`, `Meniu → Categorie`,
  `MeniuPreparat → Preparat`, `Comanda → Utilizator/StareComanda`,
  `ComandaDetaliu → Preparat/Meniu`). Acest lucru reflectă și convenția de
  soft-delete descrisă în `database/README.md`: un `Preparat`/`Meniu` folosit
  deja într-o comandă nu poate fi șters, nici prin schema SQL, nici prin EF.
- **Precizie zecimală** (`HasPrecision`) și lungimi de șir (`HasMaxLength`)
  identice cu tipurile `DECIMAL(p,s)` / `NVARCHAR(n)` / `VARCHAR(n)` din
  schemă (`Comanda.CodUnic` este mapat explicit `IsUnicode(false)` pentru că
  în schemă e `VARCHAR`, nu `NVARCHAR`).

### `DatabaseConfig.cs`
Construiește `IConfiguration` din `appsettings.json` (+ opțional
`appsettings.Development.json`) și, din connection string-ul citit de acolo,
`DbContextOptions<RestaurantDbContext>`. Connection string-ul **nu este
niciodată hardcodat în cod** — se citește la runtime din configurare, sub
cheia `ConnectionStrings:RestaurantDataNase`. Oferă și `CreateDbContext(...)`
ca metodă rapidă de a obține un `RestaurantDbContext` funcțional fără a
configura manual un container DI.

### `appsettings.json` (la rădăcina proiectului)
Conține connection string-ul implicit (`Server=localhost;Database=...`).
Copiat automat în directorul de output la build
(`CopyToOutputDirectory = PreserveNewest`, vezi `.csproj`). Pentru medii
diferite se poate adăuga `appsettings.Development.json` (opțional, e deja
căutat de `DatabaseConfig`) fără a-l versiona cu credențiale reale.

### `StoredProcedureRepository.cs`
Apelează cele 7 proceduri stocate din `schema.sql`:

| Procedură | Metodă | Cum se apelează |
|---|---|---|
| `sp_CreateComanda` | `CreateComandaAsync` | ADO.NET direct (`SqlCommand` + `SqlParameter` cu `ParameterDirection.Output`) |
| `sp_AdaugaDetaliuComanda` | `AdaugaDetaliuComandaAsync` | `ExecuteSqlInterpolatedAsync` |
| `sp_UpdateCantitateTotalaLaComanda` | `UpdateCantitateTotalaLaComandaAsync` | `ExecuteSqlInterpolatedAsync` |
| `sp_GetComenziClientCuDetalii` *(complexă)* | `GetComenziClientCuDetaliiAsync` | `FromSqlInterpolated` → `List<ComenziClientDetaliuDto>` |
| `sp_GetPreparateApropiateDeEpuizare` | `GetPreparateApropiateDeEpuizareAsync` | `FromSqlInterpolated` → `List<PreparatEpuizareDto>` |
| `sp_GetMeniuRestaurantCuAlergeni` *(complexă)* | `GetMeniuRestaurantCuAlergeniAsync` | `FromSqlInterpolated` → `List<MeniuCuAlergeniDto>` |
| `sp_SetPreparatIndisponibil` | `SetPreparatIndisponibilAsync` | `ExecuteSqlInterpolatedAsync` |

Toate metodele folosesc parametri **interpolați** (`FromSqlInterpolated` /
`ExecuteSqlInterpolatedAsync`), care EF Core îi transformă automat în
`DbParameter` reali — **niciodată** `FromSqlRaw`/`ExecuteSqlRaw` cu
concatenare de string, ca să nu existe nicio suprafață de SQL Injection.

Singura excepție este `sp_CreateComanda`: are doi parametri `OUTPUT`
(`@ComandaId`, `@CodUnic`), pe care `FromSqlInterpolated` nu îi poate
recupera. Pentru acest caz se folosește ADO.NET direct, prin
`Database.GetDbConnection()` (conexiunea gestionată de `DbContext`), cu
`SqlParameter`-i explicit `ParameterDirection.Output`; valorile parametrilor
de intrare sunt tot valori de parametru `SqlParameter`, nu text concatenat,
deci la fel de sigure.

### `Models/DTOs/`
DTO-uri pentru rezultatele procedurilor ale căror coloane nu corespund 1:1
unei entități existente: `ComenziClientDetaliuDto`, `PreparatEpuizareDto`,
`MeniuCuAlergeniDto`. Sunt înregistrate în `RestaurantDbContext` ca tipuri
**keyless** (`HasNoKey()`), nemapate pe niciun tabel/view — se populează
exclusiv prin `FromSqlInterpolated`, exact ca în exemplul oficial EF Core
pentru rezultate de proceduri stocate.

## De ce fără migrations

Schema (tabele, constrângeri, funcție, proceduri) e deja completă și
versionată în `database/schema.sql`, care e și idempotent (își face drop la
obiectele proprii înainte de a le recrea). A introduce EF Core Migrations
peste asta ar crea două surse de adevăr pentru aceeași schemă. De aceea:
- nu există folder `Migrations/`;
- `RestaurantDbContext` nu apelează `Database.Migrate()` /
  `Database.EnsureCreated()` nicăieri;
- fluxul de configurare a bazei rămâne: rulează `database/schema.sql` o
  singură dată (sau de câte ori e nevoie — e idempotent), apoi pornește
  aplicația, care doar se conectează la baza deja creată.
