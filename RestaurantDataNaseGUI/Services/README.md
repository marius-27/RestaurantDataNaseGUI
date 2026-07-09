# Services/ - autentificare, sesiune, meniul restaurantului si cautare

Acest folder contine stratul de servicii pentru autentificarea clientilor
(inregistrare + login), pentru citirea/afisarea meniului restaurantului
(preparate individuale + meniuri compuse) si pentru cautarea in meniu (dupa
denumire sau dupa alergen, cu negare). **Nu contine inca comenzile** - doar
auth, vizualizare si cautare, conform cerintelor curente.

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
  de `LoginViewModel`/`RegisterViewModel`/`MenuViewModel`/`SearchViewModel`
  cat timp proiectul nu are inca un container DI. Cand se introduce
  navigarea/DI, aceeasi instanta se poate injecta explicit in loc de
  referita prin `Instance`.

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

`GetPreparateAsync`/`GetMeniuriAsync` (metode private) si o metoda comuna
`GrupeazaPeCategorii` sunt refolosite de metodele de cautare de mai jos, ca
formatul rezultatelor (DTO-uri + grupare pe categorie) sa ramana identic
intre "afiseaza tot meniul" si "cauta in meniu":

- **`CautaDupaDenumireAsync(cuvantCheie)`** - filtreaza dupa un fragment din
  denumire, case-insensitive.
  - Pentru **preparate**, filtrul se aplica direct in interogarea EF Core,
    inainte de `ToListAsync` (`p.Denumire.ToLower().Contains(cuvantCheieLower)`),
    deci se traduce intr-un `WHERE LOWER(Denumire) LIKE ...` parametrizat de
    provider - nicio concatenare de string in SQL.
  - Pentru **meniuri**, cum `sp_GetMeniuRestaurantCuAlergeni` nu are niciun
    parametru de filtrare dupa nume, filtrul se aplica in memorie, pe lista
    deja materializata de `MeniuAfisareDto` (`string.Contains` cu
    `StringComparison.OrdinalIgnoreCase`) - tot fara nicio constructie de
    SQL din cuvantul cheie.
- **`CautaDupaAlergenAsync(numeAlergen, contineAlergen)`** - `contineAlergen = true`
  intoarce itemii care AU alergenul, `false` intoarce itemii care NU il au
  deloc.
  - Pentru **preparate**, tot in interogarea EF Core:
    `p.PreparatAlergeni.Any(pa => pa.Alergen.Denumire.ToLower() == numeAlergenLower)`
    (negat cu `!` cand `contineAlergen` e `false`), tradus de EF Core intr-un
    `EXISTS`/`NOT EXISTS` parametrizat.
  - Pentru **meniuri**, verifica in memorie daca `ListaAlergeni` (deja
    agregata de procedura din toate preparatele componente) contine
    alergenul cautat - exact semantica ceruta ("niciun preparat component nu
    contine alergenul" pentru negare).
- **`GetAlergeniDisponibiliAsync()`** - toate denumirile din `dbo.Alergen`,
  sortate alfabetic, pentru ComboBox-ul de cautare din `SearchView`.

Ambele metode de cautare re-grupeaza rezultatul cu acelasi
`GrupeazaPeCategorii`, deci daca mai multe rezultate sunt din aceeasi
categorie, categoria apare o singura data - la fel ca la afisarea normala a
meniului.

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
  `MenuView`/`SearchView` (`ItemsControl` in `ItemsControl`).
- **`SearchViewModel`**: cauta in meniu, fara sa depinda de autentificare (la
  fel ca `MenuViewModel`).
  - `CuvantCheie` (text), `AlergenSelectat` (din `AlergeniDisponibili`,
    incarcata din DB), `ContineAlergen`/`NuContineAlergen` (toggle
    bidirectional pentru "contine"/"nu contine", complementare unul altuia)
    si `TipCautare` (enum `DupaDenumire`/`DupaAlergen`, cu proprietatile
    derivate `EsteCautareDupaDenumire`/`EsteCautareDupaAlergen` folosite de
    cele doua `RadioButton` din `SearchView` - toate notificate automat prin
    `[NotifyPropertyChangedFor]` cand `TipCautare` sau `ContineAlergen` se
    schimba).
  - Comanda `IncarcaAlergeniCommand` (async) populeaza `AlergeniDisponibili`
    din `IMenuService.GetAlergeniDisponibiliAsync()`; e apelata din
    code-behind-ul `SearchView` la `Loaded`, la fel ca `IncarcaMeniuCommand`
    in `MenuView`.
  - Comanda `CautaCommand` (async, dezactivata cat timp `EsteInCurs`)
    valideaza local (cuvant cheie / alergen selectat, dupa `TipCautare`),
    apoi apeleaza `CautaDupaDenumireAsync` sau `CautaDupaAlergenAsync` din
    `IMenuService` si populeaza `RezultateCautare`
    (`ObservableCollection<CategorieGrupataViewModel>`, acelasi tip ca in
    `MenuViewModel`). Daca rezultatul e gol, seteaza `MesajNimicGasit`.
  - `EsteInCurs`/`MesajEroare` pentru starea de incarcare/eroare, la fel ca
    in `MenuViewModel`.
  - `PoateComanda` - aceeasi semantica si acelasi nume ca in `MenuViewModel`,
    fiindca template-ul de rezultate e comun celor doua View-uri (vezi mai
    jos) si se leaga de aceasta proprietate indiferent care ViewModel e
    `DataContext`-ul curent.

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

## Views/Resources noi (`Views/Resources/`)

**`MenuTemplates.axaml`** - `ResourceDictionary` (fara `x:Class`, doar
template-uri) cu cele doua `DataTemplate` reutilizate atat in `MenuView` cat
si in `SearchView`, ca sa nu existe doua copii ale aceluiasi layout:
- `MeniuAfisareItemTemplate` (`x:Key`) - cardul unui item (`MeniuAfisareDto`):
  imagine (sau placeholder), denumire, pret, cantitate/portie, alergeni ca
  text, "Indisponibil" cu rosu daca e cazul, si butonul "Comanda".
- `CategorieGrupataTemplate` (`x:Key`) - o categorie (`CategorieGrupataViewModel`):
  denumirea categoriei + un `ItemsControl` cu `WrapPanel` peste itemii ei,
  folosind `MeniuAfisareItemTemplate`.

Ambele au `x:CompileBindings="False"`: `MenuView` si `SearchView` au fiecare
un `DataContext` de alt tip (`MenuViewModel`/`SearchViewModel`), asa ca
legatura `IsVisible="{Binding DataContext.PoateComanda, ElementName=Root}"`
din `MeniuAfisareItemTemplate` nu poate fi validata static la compilare
pentru ambele tipuri deodata - foloseste bindare clasica (prin reflectie),
rezolvata la runtime prin `NameScope`-ul View-ului care instantiaza
template-ul (fiecare View isi numeste radacina `x:Name="Root"`). De aceea
ambele ViewModel-uri expun o proprietate `PoateComanda` cu exact acelasi
nume si acelasi tip - contractul care face template-ul partajabil.

## Views noi (`Views/`)

- `LoginView.axaml`(`.cs`) si `RegisterView.axaml`(`.cs`) - `UserControl`-uri
  independente, cu binding `TwoWay` pe campurile de input si comenzile de
  mai sus legate de butoane. Fiecare are un buton pentru a comuta catre
  celalalt ecran (`NavigheazaLaInregistrareCommand` / `NavigheazaLaLoginCommand`).
- `MenuView.axaml`(`.cs`) - un `ItemsControl` peste `Categorii`, cu
  `ItemTemplate="{StaticResource CategorieGrupataTemplate}"` (inclus din
  `MenuTemplates.axaml`). Singura logica din code-behind e apelul
  `IncarcaMeniuCommand` la `Loaded`.
- `SearchView.axaml`(`.cs`) - formular de cautare: doua `RadioButton` pentru
  `TipCautare` (dupa denumire / dupa alergen), un `TextBox` pentru
  `CuvantCheie` (vizibil doar la cautarea dupa denumire), un `ComboBox` cu
  `AlergeniDisponibili` + doua `RadioButton` pentru "contine"/"nu contine"
  (vizibile doar la cautarea dupa alergen), butonul "Cauta"
  (`CautaCommand`), mesaj de eroare si mesaj "nimic gasit". Rezultatele
  (`RezultateCautare`) folosesc **acelasi** `ItemTemplate="{StaticResource CategorieGrupataTemplate}"`
  ca `MenuView`, deci acelasi layout de grupare pe categorie si acelasi card
  per item. Code-behind-ul doar apeleaza `IncarcaAlergeniCommand` la
  `Loaded`, ca `ComboBox`-ul sa aiba lista de alergeni gata incarcata.

## Cum se conecteaza ulterior (nu e facut inca)

Deocamdata `LoginView`/`RegisterView`/`MenuView`/`SearchView` **nu sunt
cablate in `MainWindow`** - exista independent, ca sa poata fi verificate
separat. La pasul viitor de navigare/shell:

1. `MainWindowViewModel` va tine o proprietate gen `ViewModelBase? CurrentPage`
   (sau un `ContentControl` in `MainWindow.axaml` legat de ea prin
   `ViewLocator`-ul deja existent), plus, probabil, un buton/link catre
   `SearchView` vizibil din ecranul de meniu.
2. `MainWindowViewModel` va crea `LoginViewModel`/`RegisterViewModel`/`MenuViewModel`/`SearchViewModel`,
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
5. Pasul urmator de functionalitate (comenzile) va adauga probabil un
   `IOrderService` nou care sa foloseasca
   `StoredProcedureRepository.CreateComandaAsync`/`AdaugaDetaliuComandaAsync`,
   legat de butoanele "Comanda" deja prezente (dar fara `Command`) in
   `MeniuAfisareItemTemplate` - fara sa modifice ce exista deja aici.
