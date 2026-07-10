# SQL Server in Docker

Acest folder containerizeaza **doar baza de date** (SQL Server 2022).
Aplicatia desktop Avalonia ruleaza local pe masina si se conecteaza la
containerul de mai jos prin `localhost:1433` — GUI-ul insusi nu se
containerizeaza (nu are sens fara X11 forwarding complicat).

## 1. Configurare initiala (o singura data)

Copiaza fisierul exemplu si completeaza parola contului `sa`:

```bash
cp docker/.env.example docker/.env
```

Editeaza `docker/.env` si completeaza doua parole:

- `MSSQL_SA_PASSWORD` — parola contului `sa` (administrare/troubleshooting).
- `DB_USER_PASSWORD` — parola userului dedicat aplicatiei, `marius` (vezi
  sectiunea 5).

Cerintele SQL Server pentru parole (altfel containerul refuza sa porneasca
sau `CREATE LOGIN` esueaza):

- minim 8 caractere
- caractere din cel putin 3 din urmatoarele 4 categorii: litere mari,
  litere mici, cifre, simboluri
- nu poate fi o parola comuna/slaba

`docker/.env` **nu** se comite in git (e in `.gitignore`) — fiecare
dezvoltator isi seteaza propria parola local.

## 2. Pornirea containerului

```bash
cd docker
docker compose up -d
```

Aceasta descarca imaginea `mcr.microsoft.com/mssql/server:2022-latest` (daca
nu exista deja local), porneste containerul `restaurant-sqlserver` si
mapeaza portul `1433` catre host. Datele sunt persistate intr-un volum numit
(`sqlserver-data`), asa ca supravietuiesc unui restart al containerului.

## 3. Rularea schemei bazei de date

### Automat (recomandat)

Din radacina proiectului:

```bash
./docker/init-db.sh
```

Scriptul porneste containerul (daca nu ruleaza deja), asteapta pana cand
SQL Server e sanatos (healthcheck), ruleaza `database/schema.sql` si apoi
`docker/create-app-user.sql` (creeaza login-ul + userul `marius` si il
adauga in rolul `db_owner` pe baza de date `RestaurantDataNase`). Poate fi
rulat de mai multe ori — ambele scripturi SQL sunt idempotente.

### Manual (alternativa)

Daca preferi sa rulezi scripturile manual, dupa ce containerul e pornit si
sanatos:

```bash
docker cp database/schema.sql restaurant-sqlserver:/tmp/schema.sql
docker exec restaurant-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "<parola-sa-din-.env>" -C -i /tmp/schema.sql

docker cp docker/create-app-user.sql restaurant-sqlserver:/tmp/create-app-user.sql
docker exec restaurant-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "<parola-sa-din-.env>" -C \
  -v DB_USER_PASSWORD="<parola-marius-din-.env>" -i /tmp/create-app-user.sql
```

(Foloseste `/opt/mssql-tools/bin/sqlcmd` fara flag-ul `-C` daca imaginea ta
nu are `mssql-tools18`.)

## 4. Verificarea conexiunii

Test rapid de conexiune cu userul aplicatiei, `marius`, direct din container:

```bash
docker exec -it restaurant-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U marius -P "<parola-marius-din-.env>" -C -d RestaurantDataNase \
  -Q "SELECT name FROM sys.tables;"
```

Ar trebui sa apara lista de tabele din schema. Poti verifica acelasi lucru
si din aplicatie: seteaza connection string-ul din
`RestaurantDataNaseGUI/appsettings.Development.json` (vezi mai jos) si
porneste aplicatia — daca se conecteaza si incarca datele, totul functioneaza.

`sa` ramane disponibil separat pentru administrare/troubleshooting (ex.
inspectarea serverului, rularea din nou a `create-app-user.sql`), dar nu mai
e folosit de aplicatie.

## 5. Conectarea aplicatiei la containerul Docker

Aplicatia se conecteaza cu userul dedicat `marius` (creat de
`docker/create-app-user.sql`), nu cu `sa`.
`RestaurantDataNaseGUI/appsettings.Development.json` (fisier local,
necomis in git) contine connection string-ul potrivit pentru containerul de
mai sus:

```json
{
  "ConnectionStrings": {
    "RestaurantDataNase": "Server=localhost,1433;Database=RestaurantDataNase;User Id=marius;Password=<parola-marius-din-.env>;TrustServerCertificate=True;"
  }
}
```

Actualizeaza campul `Password` cu parola `DB_USER_PASSWORD` aleasa in
`docker/.env`, astfel incat sa corespunda. `appsettings.json` (fara
`.Development`) ramane neschimbat, cu `Trusted_Connection=True`, pentru
varianta cu SQL Server instalat local (fara Docker).

## 6. Oprire / resetare

Oprire container, **pastrand** datele (volumul ramane):

```bash
docker compose down
```

Repornire ulterioara (`docker compose up -d`) foloseste aceleasi date, fara
sa mai fie nevoie sa rulezi din nou schema.

Stergere completa, **inclusiv datele** din volum (reset total, de la zero):

```bash
docker compose down -v
```

Dupa un `down -v`, ruleaza din nou `./docker/init-db.sh` pentru a recrea
schema.
