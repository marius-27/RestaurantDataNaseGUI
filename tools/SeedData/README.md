# SeedData

Utilitar de dezvoltare care populeaza baza de date `RestaurantDataNase` cu
date de test/demo: categorii, alergeni, preparate, meniuri compuse, un cont
de angajat, doi conti de client si cateva comenzi de test.

**Nu este parte din aplicatia principala.** Nu este referentiat de
`RestaurantDataNaseGUI` si nu trebuie inclus in niciun pachet de predare sau
build de productie. Este doar un script rulat manual, o singura data sau ori
de cate ori vrei sa repopulezi baza de date de dezvoltare.

## Cand se ruleaza

Dupa ce:

1. `database/schema.sql` a fost aplicat pe baza de date.
2. `docker/create-app-user.sql` (userul de aplicatie) a fost aplicat.
3. Containerul Docker cu SQL Server este pornit si accesibil.

## Cum se ruleaza

```bash
cd tools/SeedData
dotnet run
```

Scriptul foloseste `DatabaseConfig.CreateDbContext()` din
`RestaurantDataNaseGUI`, exact ca aplicatia principala - `appsettings.json`
si `appsettings.Development.json` din acest folder sunt legate (link, nu
copie) la fisierele din `RestaurantDataNaseGUI/`, deci scrie mereu in aceeasi
baza de date pe care o foloseste GUI-ul.

## Ce populeaza

- Categorii: Mic dejun, Aperitive, Supe si ciorbe, Fel principal, Deserturi, Bauturi.
- Alergeni: Gluten, Oua, Lactoza, Telina, Peste, Fructe de mare, Soia, Arahide.
- Un cont de angajat: `angajat@restaurant.ro` / `Angajat123!`.
- Doi conti de client (prin fluxul real `AuthService.RegisterAsync`):
  `client1@test.ro` / `Client123!` si `client2@test.ro` / `Client123!`.
- 21 de preparate distribuite pe toate categoriile, cu alergeni asociati unde
  are sens; cateva marcate `Disponibil = false`, si 3 cu stoc sub pragul de
  epuizare (`PragStocEpuizare` din `dbo.Configurare`), ca sa poti demonstra
  alerta de stoc pentru angajat.
- 4 meniuri compuse din preparatele de mai sus.
- 3 comenzi de test pentru `client1@test.ro`, in stari diferite
  (`inregistrata`, `se pregateste`, `livrata`), ca sa poti demonstra
  `MyOrdersView` cu date reale.

## Idempotenta

Scriptul verifica, dupa denumire/email, ce exista deja in baza de date si
sare peste entitatile deja create - poate fi rulat de mai multe ori fara sa
duplice date. Comenzile de test se genereaza o singura data (daca
`client1@test.ro` are deja comenzi, pasul e omis in intregime).

## Nota despre contul de angajat

`AuthService`/`AdminService` nu au un flux de inregistrare pentru
`TipUtilizator = "Angajat"` (nu exista in aplicatia reala - angajatii nu se
auto-inregistreaza). De aceea, doar acest script insereaza direct un
`Utilizator` cu `TipUtilizator = "Angajat"` prin `RestaurantDbContext`, cu
parola hash-uita via `BCrypt.Net.BCrypt.HashPassword`. E acceptabil aici
pentru ca scriptul de seed nu trebuie sa treaca prin validarile de business
ale unui flux real de inregistrare - dar acest pattern nu trebuie copiat in
codul aplicatiei principale.
