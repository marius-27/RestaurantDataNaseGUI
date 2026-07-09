# Services/ - autentificare si sesiune

Acest folder contine stratul de servicii pentru autentificarea clientilor
(inregistrare + login). **Nu contine meniul restaurantului sau comenzile** -
doar auth, conform cerintei curente.

## Fisiere

### `IAuthService.cs` / `AuthService.cs`
- `RegisterAsync(nume, prenume, email, telefon, adresaLivrare, parola)` -
  creeaza un `Utilizator` nou cu `TipUtilizator = "Client"`.
  - Valideaza: nume/prenume nevide, email cu format valid (regex), telefon
    nevid, parola cu minim 8 caractere.
  - Verifica unicitatea email-ului cu o interogare EF Core LINQ
    (`Utilizatori.AnyAsync(u => u.Email == email)`) **inainte** de insert.
    Ca fallback pentru cursa dintre verificare si insert (doi useri se
    inregistreaza simultan cu acelasi email), prinde si `DbUpdateException`
    generata de constrangerea `UQ_Utilizator_Email` din baza de date
    (`SqlException.Number` 2601/2627).
- `LoginAsync(email, parola)` - cauta utilizatorul dupa email
  (`FirstOrDefaultAsync`, tot LINQ) si verifica parola cu `BCrypt.Verify`.
- Ambele returneaza un `AuthResult` (`Succes`, `Utilizator?`, `MesajEroare?`)
  in loc sa arunce exceptii pentru cazurile "asteptate" (email duplicat,
  credentiale gresite).
- **Parola nu e niciodata stocata sau comparata in clar.** Se foloseste
  pachetul `BCrypt.Net-Next` (`BCrypt.HashPassword` / `BCrypt.Verify`), care
  include salt automat per-hash - nu un simplu SHA256.
- Fiecare metoda deschide propriul `RestaurantDbContext` de scurta durata
  (via `Func<RestaurantDbContext>` injectat in constructor, implicit
  `DatabaseConfig.CreateDbContext()`), la fel ca restul stratului de date -
  DbContext nu e thread-safe si nu trebuie tinut deschis pe durata rularii
  aplicatiei.
- Toate interogarile sunt LINQ peste EF Core (parametrizate automat de
  provider), nu SQL brut - fara suprafata de SQL Injection.

### `ISessionService.cs` / `SessionService.cs`
Tine minte in memorie utilizatorul autentificat curent, cat timp ruleaza
aplicatia (nu persista pe disc):
- `CurrentUser` - utilizatorul curent sau `null`.
- `EsteAutentificat`, `EsteAngajat`, `EsteClient` - verificari rapide bazate
  pe `TipUtilizator`.
- `CurrentUserChanged` - eveniment declansat la login/logout, ca ViewModel-
  urile (sau un shell viitor) sa poata reactiona fara polling.
- `SessionService.Instance` - un singleton static simplu, folosit implicit
  de `LoginViewModel`/`RegisterViewModel` cat timp proiectul nu are inca un
  container DI. Cand se introduce navigarea/DI, aceeasi instanta se poate
  injecta explicit in loc de referita prin `Instance`.

## ViewModels noi (`ViewModels/`)

- **`LoginViewModel`**: proprietati bindabile `Email`, `Parola`,
  `MesajEroare`, `EsteInCurs`; comanda `LoginCommand` (dezactivata automat
  cat timp `EsteInCurs`) care apeleaza `IAuthService.LoginAsync`, actualizeaza
  `ISessionService` la succes si ridica evenimentul `LoginReusit` (cu
  `Utilizator`-ul autentificat) pentru ca un shell viitor sa navigheze mai
  departe. Comanda `NavigheazaLaInregistrareCommand` ridica
  `NavigheazaLaInregistrareRequested`.
- **`RegisterViewModel`**: aceleasi campuri ca `Utilizator` (nume, prenume,
  email, telefon, adresa livrare optionala) plus `Parola`/`ConfirmareParola`.
  Valideaza local (email valid, telefon valid, parola >= 8 caractere,
  parolele coincid) inainte sa apeleze `IAuthService.RegisterAsync`; la
  succes actualizeaza sesiunea si ridica `InregistrareReusita`. Comanda
  `NavigheazaLaLoginCommand` ridica `NavigheazaLaLoginRequested`.

Ambele au un constructor implicit (folosit de `Design.DataContext` din
XAML si de `ViewLocator`) care instantiaza direct `AuthService` +
`SessionService.Instance`, si un al doilea constructor care accepta
`IAuthService`/`ISessionService` explicit, util pentru teste sau pentru
injectie ulterioara.

## Views noi (`Views/`)

`LoginView.axaml`(`.cs`) si `RegisterView.axaml`(`.cs`) - `UserControl`-uri
independente, cu binding `TwoWay` pe campurile de input si comenzile de mai
sus legate de butoane. Fiecare are un buton pentru a comuta catre celalalt
ecran (`NavigheazaLaInregistrareCommand` / `NavigheazaLaLoginCommand`).

## Cum se conecteaza ulterior (nu e facut inca)

Deocamdata `LoginView`/`RegisterView` **nu sunt cablate in `MainWindow`** -
exista independent, ca sa poata fi verificate separat. La pasul viitor de
navigare/shell:

1. `MainWindowViewModel` va tine o proprietate gen `ViewModelBase? CurrentPage`
   (sau un `ContentControl` in `MainWindow.axaml` legat de ea prin
   `ViewLocator`-ul deja existent).
2. `MainWindowViewModel` va crea `LoginViewModel`/`RegisterViewModel`, se va
   abona la `LoginReusit` / `InregistrareReusita` (ca sa navigheze catre
   ecranul urmator, de ex. meniul restaurantului) si la
   `NavigheazaLaInregistrareRequested` / `NavigheazaLaLoginRequested` (ca sa
   comute intre cele doua ecrane de auth).
3. Cand se introduce un container DI (sau macar o clasa de "composition
   root"), `AuthService`/`SessionService.Instance` se pot inregistra ca
   singletons si injecta explicit in `LoginViewModel`/`RegisterViewModel` in
   loc de constructorul implicit fara parametri.
4. `SessionService.Instance.EsteAngajat` / `EsteClient` se pot folosi atunci
   in `MainWindowViewModel` ca sa decida ce ecran urmeaza dupa autentificare
   (meniu client vs. panou angajat) - fara sa se reimplementeze auth-ul.
