# Services/ - autentificare, sesiune si meniul restaurantului

Acest folder contine stratul de servicii pentru autentificarea clientilor
(inregistrare + login) si pentru citirea/afisarea meniului restaurantului
(preparate individuale + meniuri compuse). **Nu contine inca cautarea in
meniu si nici comenzile** - doar auth si vizualizare, conform cerintelor
curente.

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
  de `LoginViewModel`/`RegisterViewModel`/`MenuViewModel` cat timp proiectul
  nu are inca un container DI. Cand se introduce navigarea/DI, aceeasi
  instanta se poate injecta explicit in loc de referita prin `Instance`.

### `IMenuService.cs` / `MenuService.cs`
`GetMeniuRestaurantAsync()` returneaza toate categoriile din meniu
(`List<CategorieMeniuDto>`), fiecare cu preparatele si meniurile aferente
deja grupate si sortate alfabetic. Combina doua surse, pe acelasi
`RestaurantDbContext` (secvential, nu in paralel - un `DbContext` nu suporta
operatii async concurente):
- **Preparate individuale** - interogare EF Core LINQ directa, cu
  `Include(p => p.Categorie)`, `Include(p => p.PreparatAlergeni).ThenInclude(pa => pa.Alergen)`
  si `Include(p => p.Imagini)`, mapate apoi (in memorie) pe `MeniuAfisareDto`.
- **Meniuri compuse** - `StoredProcedureRepository.GetMeniuRestaurantCuAlergeniAsync()`,
  adica procedura stocata `dbo.sp_GetMeniuRestaurantCuAlergeni`, care da
  pretul calculat dinamic (`dbo.fn_CalculeazaPretMeniu`) si alergenii deja
  agregati din toate preparatele componente. Procedura nu returneaza si
  disponibilitatea, asa ca aceasta se calculeaza separat printr-o interogare
  LINQ (`m.MeniuPreparate.Any(mp => !mp.Preparat.Disponibil)`).

Preparatele si meniurile **indisponibile nu sunt excluse din rezultat** -
sunt incluse cu `EsteIndisponibil = true`, ca View-ul sa poata afisa
"Indisponibil" langa ele (cerinta explicita).

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

- **`MenuViewModel`**: proprietatea bindabila `Categorii`
  (`ObservableCollection<CategorieGrupataViewModel>`), plus `EsteInCurs` si
  `MesajEroare` pentru starea de incarcare/eroare. Comanda
  `IncarcaMeniuCommand` (async) apeleaza `IMenuService.GetMeniuRestaurantAsync()`
  si populeaza `Categorii`; e apelata din code-behind-ul `MenuView`, la
  evenimentul `Loaded` (lazy load la afisare), nu din View direct prin
  binding, ca sa nu fie nevoie de logica suplimentara in XAML. Proprietatea
  `PoateComanda` (`EsteAutentificat && EsteClient` din `ISessionService`) e
  folosita **doar** ca View-ul sa decida daca arata butonul "Comanda" -
  meniul insusi se incarca si se afiseaza indiferent daca userul e
  autentificat sau nu (cerinta explicita: un vizitator fara cont trebuie sa
  vada meniul).
- **`CategorieGrupataViewModel`**: `Denumire` + `ObservableCollection<MeniuAfisareDto>`
  `Itemi` - o categorie din meniu, gata grupata pentru binding-ul din
  `MenuView` (`ItemsControl` in `ItemsControl`).

## DTOs noi (`Models/DTOs/`)

- **`MeniuAfisareDto`**: model unificat pentru UI - reprezinta fie un
  `Preparat`, fie un `Meniu` compus. Contine `Denumire`, `Categorie`, `Pret`,
  `CantitatePortie`/`UnitateMasura` (null pentru meniuri compuse, care nu au
  o singura portie), `ListaAlergeni`, `ListaImaginiPath` (goala pentru
  meniuri - `Meniu` nu are imagine proprie in schema), `EsteMeniuCompus` si
  `EsteIndisponibil`. Are si proprietati calculate pentru binding direct in
  XAML fara convertoare suplimentare: `PrimaImaginePath`, `CantitateText`,
  `AlergeniText`.
- **`CategorieMeniuDto`**: `Denumire` + `IReadOnlyList<MeniuAfisareDto> Itemi` -
  rezultatul brut al `IMenuService.GetMeniuRestaurantAsync()`, inainte de a
  fi impachetat in `CategorieGrupataViewModel` (colectii bindabile).

## Converters noi (`Converters/`)

**`CaleImagineToBitmapConverter`**: incarca un `Bitmap` Avalonia dintr-o cale
de fisier (`PreparatImagine.CalePoza`); daca fisierul nu exista sau nu poate
fi incarcat, returneaza `null` in loc sa arunce o exceptie, iar `MenuView`
afiseaza un placeholder text ("Fara imagine") in acel caz.

## Views noi (`Views/`)

- `LoginView.axaml`(`.cs`) si `RegisterView.axaml`(`.cs`) - `UserControl`-uri
  independente, cu binding `TwoWay` pe campurile de input si comenzile de
  mai sus legate de butoane. Fiecare are un buton pentru a comuta catre
  celalalt ecran (`NavigheazaLaInregistrareCommand` / `NavigheazaLaLoginCommand`).
- `MenuView.axaml`(`.cs`) - listeaza categoriile (`ItemsControl` exterior)
  si, pentru fiecare categorie, itemii ei intr-un `WrapPanel` (`ItemsControl`
  interior): imagine (sau placeholder), denumire, pret, cantitate/portie,
  alergeni ca text si, daca `EsteIndisponibil`, un text rosu "Indisponibil".
  Butonul "Comanda" e vizibil doar cand `MenuViewModel.PoateComanda` e true
  (legat prin `ElementName=Root` catre `DataContext` al `UserControl`-ului
  radacina, fiindca template-ul itemului are propriul `DataContext` de tip
  `MeniuAfisareDto`) si e dezactivat cand itemul e indisponibil; nu are inca
  un `Command` legat - comenzile propriu-zise sunt un pas viitor. Singura
  logica din code-behind e apelul `IncarcaMeniuCommand` la `Loaded`.

## Cum se conecteaza ulterior (nu e facut inca)

Deocamdata `LoginView`/`RegisterView`/`MenuView` **nu sunt cablate in
`MainWindow`** - exista independent, ca sa poata fi verificate separat. La
pasul viitor de navigare/shell:

1. `MainWindowViewModel` va tine o proprietate gen `ViewModelBase? CurrentPage`
   (sau un `ContentControl` in `MainWindow.axaml` legat de ea prin
   `ViewLocator`-ul deja existent).
2. `MainWindowViewModel` va crea `LoginViewModel`/`RegisterViewModel`/`MenuViewModel`,
   se va abona la `LoginReusit` / `InregistrareReusita` (ca sa navigheze
   catre ecranul urmator, ex. meniul restaurantului) si la
   `NavigheazaLaInregistrareRequested` / `NavigheazaLaLoginRequested` (ca sa
   comute intre cele doua ecrane de auth). `MenuView` va fi probabil ecranul
   implicit/de start, accesibil si fara autentificare.
3. Cand se introduce un container DI (sau macar o clasa de "composition
   root"), `AuthService`/`MenuService`/`SessionService.Instance` se pot
   inregistra ca singletons si injecta explicit in ViewModels in loc de
   constructorul implicit fara parametri.
4. `SessionService.Instance.EsteAngajat` / `EsteClient` se pot folosi atunci
   in `MainWindowViewModel` ca sa decida ce ecran urmeaza dupa autentificare
   (meniu client vs. panou angajat) - fara sa se reimplementeze auth-ul sau
   afisarea meniului.
5. Pasul urmator de functionalitate (cautare + comenzi) va extinde probabil
   `IMenuService`/`MenuViewModel` cu filtrare, si va adauga un `IOrderService`
   nou care sa foloseasca `StoredProcedureRepository.CreateComandaAsync`/`AdaugaDetaliuComandaAsync`
   - fara sa modifice ce exista deja aici.
