# RestaurantDataNaseGUI

Aplicație desktop pentru gestiunea unui restaurant, construită cu
[Avalonia UI](https://avaloniaui.net/) (.NET 10) și SQL Server. Permite
administrarea meniului (categorii, preparate, alergeni, meniuri compuse),
plasarea și urmărirea comenzilor, gestionarea conturilor de
clienți/angajați și vizualizarea de rapoarte simple (stocuri aproape de
epuizare, istoric comenzi).

## Structura proiectului

| Folder / proiect | Rol |
|---|---|
| `RestaurantDataNaseGUI/` | Aplicația desktop Avalonia (MVVM: `Views`, `ViewModels`, `Services`, `Data`) |
| `RestaurantDataNaseGUI.test/` | Teste unitare (SQLite in-memory) și teste de integrare (SQL Server real) |
| `tools/SeedData/` | Utilitar de dezvoltare pentru populare cu date demo (categorii, alergeni, preparate, meniuri, conturi, comenzi) |
| `docker/` | SQL Server 2022 containerizat via Docker Compose, pentru dezvoltare locală |
| `database/` | Schema SQL a bazei de date (`schema.sql`): tabele, constrângeri, funcție și proceduri stocate |
| `k8s/` | Manifeste Kubernetes echivalente lui `docker/`, pentru demonstrație/completare |

## Cerințe

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) + Docker Compose (pentru baza de date locală)
- Opțional: `kubectl` + un cluster local (minikube/kind), dacă folosești varianta Kubernetes din `k8s/` în loc de Docker

## Pornire rapidă

### 1. Clonare

```bash
git clone <url-repo>
cd Restaurant
```

### 2. Configurare și pornire bază de date (Docker)

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

Scriptul așteaptă ca SQL Server să fie sănătos, apoi rulează
`database/schema.sql` și `docker/create-app-user.sql` (creează userul
dedicat aplicației, `marius`). Detalii complete în [docker/README.md](docker/README.md).

### 4. Configurare connection string

Creează `RestaurantDataNaseGUI/appsettings.Development.json` (fișier local,
necomis în git) cu connection string-ul către containerul de mai sus:

```json
{
  "ConnectionStrings": {
    "RestaurantDataNase": "Server=localhost,14330;Database=RestaurantDataNase;User Id=marius;Password=<parola-marius-din-.env>;TrustServerCertificate=True;"
  }
}
```

### 5. (Opțional) Populare cu date demo

```bash
cd tools/SeedData
dotnet run
cd ../..
```

Vezi [tools/SeedData/README.md](tools/SeedData/README.md) pentru ce anume populează.

### 6. Rulare aplicație

```bash
dotnet run --project RestaurantDataNaseGUI
```

## Rulare teste

Proiectul de teste conține două categorii:

- **Teste unitare** — folosesc SQLite in-memory, izolate per test, nu necesită nicio configurare externă.
- **Teste de integrare** (`Category=Integration`) — rulează împotriva unui SQL Server real (containerul din `docker/`, cu schema deja aplicată) și necesită variabila de mediu `RESTAURANT_TEST_CONNECTION_STRING`.

Doar teste unitare (fără bază de date reală):

```bash
dotnet test --filter "Category!=Integration"
```

Toate testele, inclusiv cele de integrare:

```bash
export RESTAURANT_TEST_CONNECTION_STRING="Server=localhost,14330;Database=RestaurantDataNase;User Id=marius;Password=<parola-ta>;TrustServerCertificate=True;"
dotnet test
```

## Documentație suplimentară

- [docker/README.md](docker/README.md) — configurare completă SQL Server în Docker Compose
- [database/README.md](database/README.md) — schema bazei de date, tabele, funcție și proceduri stocate
- [k8s/README.md](k8s/README.md) — deployment alternativ pe Kubernetes
- [tools/SeedData/README.md](tools/SeedData/README.md) — utilitarul de populare cu date demo

## Tehnologii folosite

- [Avalonia UI](https://avaloniaui.net/) 12 (.NET 10, desktop, MVVM cu CommunityToolkit.Mvvm)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/) (SQL Server provider)
- [SQL Server 2022](https://www.microsoft.com/sql-server)
- [xUnit](https://xunit.net/) + Moq, pentru teste unitare și de integrare
- BCrypt.Net, pentru hash-uirea parolelor