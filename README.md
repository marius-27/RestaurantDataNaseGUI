# RestaurantDataNaseGUI

Aplicatie desktop pentru gestiunea unui restaurant, construita cu
[Avalonia UI](https://avaloniaui.net/) (.NET 10) si SQL Server. Permite
administrarea meniului (categorii, preparate, alergeni, meniuri compuse),
plasarea si urmarirea comenzilor, gestionarea conturilor de
clienti/angajati si vizualizarea de rapoarte simple (stocuri aproape de
epuizare, istoric comenzi).

## Structura proiectului

| Folder / proiect | Rol |
|---|---|
| `RestaurantDataNaseGUI/` | Aplicatia desktop Avalonia (MVVM: `Views`, `ViewModels`, `Services`, `Data`) |
| `RestaurantDataNaseGUI.test/` | Teste unitare (SQLite in-memory) si teste de integrare (SQL Server real) |
| `tools/SeedData/` | Utilitar de dezvoltare pentru populare cu date demo (categorii, alergeni, preparate, meniuri, conturi, comenzi) |
| `docker/` | SQL Server 2022 containerizat via Docker Compose, pentru dezvoltare locala |
| `database/` | Schema SQL a bazei de date (`schema.sql`): tabele, constrangeri, functie si proceduri stocate |
| `k8s/` | Manifeste Kubernetes echivalente lui `docker/`, pentru demonstratie/completare |

## Cerinte

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) + Docker Compose (pentru baza de date locala)
- Optional: `kubectl` + un cluster local (minikube/kind), daca folosesti varianta Kubernetes din `k8s/` in loc de Docker

## Pornire rapida

### 1. Clonare

```bash
git clone <url-repo>
cd Restaurant
```

### 2. Configurare si pornire baza de date (Docker)

```bash
cp docker/.env.example docker/.env
# editeaza docker/.env si completeaza MSSQL_SA_PASSWORD / DB_USER_PASSWORD

cd docker
docker compose up -d
cd ..
```

### 3. Rularea schemei bazei de date

```bash
./docker/init-db.sh
```

Scriptul asteapta ca SQL Server sa fie sanatos, apoi ruleaza
`database/schema.sql` si `docker/create-app-user.sql` (creeaza userul
dedicat aplicatiei, `marius`). Detalii complete in [docker/README.md](docker/README.md).

### 4. Configurare connection string

Creeaza `RestaurantDataNaseGUI/appsettings.Development.json` (fisier local,
necomis in git) cu connection string-ul catre containerul de mai sus:

```json
{
  "ConnectionStrings": {
    "RestaurantDataNase": "Server=localhost,14330;Database=RestaurantDataNase;User Id=marius;Password=<parola-marius-din-.env>;TrustServerCertificate=True;"
  }
}
```

### 5. (Optional) Populare cu date demo

```bash
cd tools/SeedData
dotnet run
cd ../..
```

Vezi [tools/SeedData/README.md](tools/SeedData/README.md) pentru ce anume populeaza.

### 6. Rulare aplicatie

```bash
dotnet run --project RestaurantDataNaseGUI
```

## Rulare teste

Proiectul de teste contine doua categorii:

- **Teste unitare** — folosesc SQLite in-memory, izolate per test, nu necesita nicio configurare externa.
- **Teste de integrare** (`Category=Integration`) — ruleaza impotriva unui SQL Server real (containerul din `docker/`, cu schema deja aplicata) si necesita variabila de mediu `RESTAURANT_TEST_CONNECTION_STRING`.

Doar teste unitare (fara baza de date reala):

```bash
dotnet test --filter "Category!=Integration"
```

Toate testele, inclusiv cele de integrare:

```bash
export RESTAURANT_TEST_CONNECTION_STRING="Server=localhost,14330;Database=RestaurantDataNase;User Id=marius;Password=<parola-ta>;TrustServerCertificate=True;"
dotnet test
```

## Documentatie suplimentara

- [docker/README.md](docker/README.md) — configurare completa SQL Server in Docker Compose
- [database/README.md](database/README.md) — schema bazei de date, tabele, functie si proceduri stocate
- [k8s/README.md](k8s/README.md) — deployment alternativ pe Kubernetes
- [tools/SeedData/README.md](tools/SeedData/README.md) — utilitarul de populare cu date demo

## Tehnologii folosite

- [Avalonia UI](https://avaloniaui.net/) 12 (.NET 10, desktop, MVVM cu CommunityToolkit.Mvvm)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) (SQL Server provider)
- [SQL Server 2022](https://www.microsoft.com/sql-server)
- [xUnit](https://xunit.net/) + Moq, pentru teste unitare si de integrare
- BCrypt.Net, pentru hash-uirea parolelor
