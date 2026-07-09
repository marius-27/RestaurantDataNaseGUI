# Services/ - autentificare, sesiune, meniu, cautare, cos si comenzi

Acest folder contine stratul de servicii pentru autentificarea clientilor
(inregistrare + login), pentru citirea/afisarea meniului restaurantului
(preparate individuale + meniuri compuse), pentru cautarea in meniu (dupa
denumire sau dupa alergen, cu negare), pentru cosul de comanda (adaugare din
meniu/cautare, cantitate editabila) si pentru **crearea, vizualizarea si
anularea** comenzilor (doar pentru clienti autentificati). **Nu contine inca
schimbarea starii unei comenzi** (ex. spre "se pregateste") - asta apartine
modulului de angajat, pas viitor separat (vezi ultima sectiune).

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
  de `LoginViewModel`/`RegisterViewModel`/`MenuViewModel`/`SearchViewModel`/`CartViewModel`/`MyOrdersViewModel`
  cat timp proiectul nu are inca un container DI. Cand se introduce
  navigarea/DI, aceeasi instanta se poate injecta explicit in loc de
  referita prin `Instance`.

### `ICartService.cs` / `CartService.cs`
Cosul de comanda al sesiunii curente - in-memory, simplu, la fel ca
`SessionService` (nu persista pe disc, un singur cos per rulare a
aplicatiei):
- `Articole` (`ObservableCollection<ArticolCosDto>`) - colectia partajata;
  `MenuViewModel`/`SearchViewModel` adauga in ea, `CartViewModel` doar o
  citeste (aceeasi referinta, nu o copie).
- `AdaugaInCos(item, cantitate)` - comoditate care construieste un
  `ArticolCosDto` dintr-un `MeniuAfisareDto` afisat in meniu/cautare (decide
  singur daca merge pe `PreparatId` sau `MeniuId` din `EsteMeniuCompus`) si
  il adauga; `AdaugaArticol(articol)` e varianta de nivel jos, folosita si de
  `AdaugaInCos` intern - daca exista deja un articol pentru acelasi
  Preparat/Meniu, ii aduna cantitatile in loc sa creeze o linie noua.
- `ModificaCantitate(articol, cantitateNoua)` - seteaza cantitatea; daca
  rezultatul e `<= 0`, sterge articolul (nu lasa linii cu cantitate 0/negativa).
- `StergeArticol`/`GolesteCos` - elimina un articol / goleste tot cosul.
- `CosSchimbat` - eveniment declansat de **toate** metodele de mai sus
  (inclusiv `ModificaCantitate`, care nu schimba colectia in sine, doar o
  proprietate a unui item existent) - `CartViewModel` se bazeaza exclusiv pe
  acest eveniment ca sa stie cand sa recalculeze costul, fara sa asculte
  separat fiecare articol.
- **Se goleste automat la logout**: `CartService` se aboneaza la
  `ISessionService.CurrentUserChanged` si cheama `GolesteCos()` cand
  `EsteAutentificat` devine `false` - cosul unui client nu trebuie sa
  supravietuiasca sesiunii lui (si nici sa fie vizibil urmatorului user care
  se autentifica pe aceeasi instanta a aplicatiei).
- `CartService.Instance` - singleton static, exact acelasi rol ca
  `SessionService.Instance`.

`ArticolCosDto` (in `Models/DTOs/`) a devenit observabil (`ObservableObject`
din CommunityToolkit.Mvvm) special pentru cos: `CartView` editeaza
`Cantitate` prin `CartViewModel.ModificaCantitateCommand`, care cheama
`ICartService.ModificaCantitate` - acesta seteaza direct proprietatea
observabila, care isi notifica automat si proprietatea calculata `Subtotal`
(`PretUnitar * Cantitate`), deci linia din `CartView` se actualizeaza fara
niciun cod suplimentar de sincronizare.

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

### `IOrderService.cs` / `OrderService.cs`
Calculul de cost si crearea comenzilor, **doar pentru clienti autentificati**.
Toate pragurile/procentele se citesc din `dbo.Configurare` la fiecare calcul
(`CitesteConfigurareAsync`, metoda privata) - niciodata hardcodate in cod.

**Reguli de discount si transport** (vezi seed-ul din `database/schema.sql`,
tabelul `dbo.Configurare`):

| Regula | Cheie(i) din `Configurare` |
|---|---|
| Discount daca subtotalul > suma minima | `SumaMinimaComandaDiscount` (100 lei implicit) |
| Discount daca clientul are peste N comenzi in ultimele T zile | `NumarComenziPentruDiscount` (5), `IntervalTimpDiscount` (30 zile) |
| Procentul de discount (comun ambelor conditii de mai sus) | `ProcentDiscountFrecventa` (5%) |
| Cost transport standard, sub pragul de transport gratuit | `CostTransport` (15 lei) |
| Prag peste care transportul e gratuit | `PragTransportGratuit` (150 lei) |

Detalii importante de implementare:
- Schema **nu are un procent de discount separat** pentru "comanda mare" -
  exista o singura cheie de procent, `ProcentDiscountFrecventa`. De aceea
  ambele conditii (subtotal mare / client frecvent) folosesc acelasi procent
  cand se aplica.
- **Discounturile nu se cumuleaza**: daca ambele conditii sunt indeplinite
  simultan, discountul tot se aplica o singura data (acelasi procent oricum,
  fiindca nu exista doua chei diferite) - `CalculComandaDto.MotivDiscount`
  descrie in text care conditie/conditii s-au indeplinit, pentru afisare.
- "Client frecvent" = strict mai multe comenzi decat
  `NumarComenziPentruDiscount` (`COUNT(...) > z`, nu `>=`), plasate cu
  `DataComanda >= UtcNow - IntervalTimpDiscount zile` - interogare EF Core
  LINQ (`context.Comenzi.CountAsync(...)`), parametrizata automat.
- Costul de transport se calculeaza pe `SubtotalMancare` (inainte de
  discount): daca e sub `PragTransportGratuit`, se aplica `CostTransport`;
  altfel transport gratuit.

**Metode**:
- `CalculeazaCostComandaAsync(articole, utilizatorId)` - returneaza un
  `CalculComandaDto` (subtotal, procent + valoare + motiv discount, cost
  transport, total), fara sa creeze nicio comanda. Reutilizabila dintr-un
  viitor ecran de "cos" ca sa se afiseze totalul inainte de confirmare.
- `CreeazaComandaAsync(articole, utilizatorId)`:
  1. Verifica prin `ISessionService` ca exista un utilizator autentificat,
     ca e `Client` (nu `Angajat`), **si** ca `utilizatorId` dat ca parametru
     chiar corespunde utilizatorului curent din sesiune (altfel un apel
     gresit/rau-intentionat ar putea crea o comanda in numele altcuiva) -
     orice esec aici intoarce un `OrderResult` cu `Succes = false`, fara sa
     arunce exceptie.
  2. Valideaza cosul: nu e gol, fiecare articol are exact unul dintre
     `PreparatId`/`MeniuId`, cantitati pozitive.
  3. **Verifica disponibilitatea** fiecarui articol prin EF Core LINQ: un
     preparat trebuie sa aiba `Disponibil = true`; un meniu trebuie sa aiba
     **toate** preparatele lui componente disponibile
     (`m.MeniuPreparate.All(mp => mp.Preparat.Disponibil)`) - daca nu,
     `OrderResult.Esec(...)` cu un mesaj clar, fara sa creeze nimic.
  4. Calculeaza costurile prin `CalculeazaCostComandaAsync` (reutilizat
     intern, pe acelasi `DbContext`).
  5. Deschide o tranzactie EF Core (`context.Database.BeginTransactionAsync`),
     apeleaza `StoredProcedureRepository.CreateComandaAsync` (antetul
     comenzii, cu costul de transport si procentul de discount calculate) si
     apoi `AdaugaDetaliuComandaAsync` pentru fiecare articol din cos; daca
     orice pas esueaza, `RollbackAsync` complet - crearea comenzii e atomica.
- `GetComenziClientAsync(utilizatorId)` - toate comenzile clientului, cu
  articolele lor, cele mai recente primele:
  - `StoredProcedureRepository.GetComenziClientCuDetaliiAsync(utilizatorId)`
    (procedura `dbo.sp_GetComenziClientCuDetalii`) returneaza **un rand per
    articol de comanda** (JOIN intre `Comanda`/`StareComanda`/`ComandaDetaliu`/`Preparat`/`Meniu`),
    deci trebuie grupat: `randuri.GroupBy(r => r.ComandaId)`, apoi fiecare
    grup devine un `ComandaClientDto` (antetul comenzii, luat de pe primul
    rand al grupului) cu `Articole` = lista de `ArticolComandaClientDto`
    (denumire + cantitate + pret unitar) construita din toate randurile
    grupului.
  - `SubtotalMancare` = suma `SubTotal`-urilor randurilor; `Total` se
    reconstruieste din snapshot-ul istoric al comenzii (`SubtotalMancare -
    SubtotalMancare * Discount / 100 + CostTransport`) - **nu** se recalculeaza
    regulile de business curente, fiindca `Discount`/`CostTransport` sunt deja
    inghetate pe `Comanda` de la creare (schimbarea ulterioara a valorilor din
    `Configurare` nu trebuie sa modifice retroactiv comenzi vechi).
  - `EsteActiva` = starea comenzii nu e `"livrata"` sau `"anulata"` (setul
    `StariFinale`, comparatie case-insensitive).
- `AnuleazaComandaAsync(comandaId, utilizatorId)`:
  1. Aceeasi verificare de sesiune ca la `CreeazaComandaAsync` (autentificat,
     `Client`, `utilizatorId` == userul curent).
  2. Incarca `Comanda` (cu `Include(c => c.Stare)`) si verifica prin EF Core
     ca **chiar apartine** lui `utilizatorId` - altfel `OrderResult.Esec(...)`,
     nu se schimba nimic.
  3. Verifica ca starea curenta **nu** e deja `"livrata"`/`"anulata"`
     (aceeasi lista `StariFinale`) - o comanda finala nu mai poate fi anulata.
  4. Cauta Id-ul starii `"anulata"` in `dbo.StareComanda` si il seteaza direct
     pe entitatea `Comanda` (`comanda.StareId = ...`), apoi
     `context.SaveChangesAsync()` - **un simplu `UPDATE` prin EF Core**, nu o
     procedura stocata (schema nu are una dedicata pentru asta).
- Toate cele patru metode intorc/folosesc `OrderResult`/`CalculComandaDto`/`List<ComandaClientDto>`
  in loc sa propage exceptii pentru cazurile "asteptate" (neautentificat, cos
  gol, articol indisponibil, comanda deja finala), la fel ca `AuthResult` din
  `AuthService`.

**Ce NU face inca `OrderService`** (pas viitor, alt modul):
- Nu schimba starea unei comenzi catre **"se pregateste"** (doar catre
  `"anulata"`, prin `AnuleazaComandaAsync`). Cand o comanda trece in "se
  pregateste" - lucru care apartine modulului de angajat
  (`feature/employee-features`) - acel modul trebuie sa apeleze
  `StoredProcedureRepository.UpdateCantitateTotalaLaComandaAsync(comandaId)`,
  ca sa scada din stoc cantitatile consumate de comanda. Aici, la creare
  (starea ramane `"inregistrata"`, pusa de `sp_CreateComanda`), stocul nu e
  atins.

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
  vada meniul). Comanda `AdaugaInCosCommand` (parametru: `MeniuAfisareDto`)
  e legata de butonul "Comanda" din template; nu face nimic daca
  `!PoateComanda` sau daca itemul e `EsteIndisponibil`, altfel cheama
  `ICartService.AdaugaInCos(item)`.
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
  - `PoateComanda` si `AdaugaInCosCommand` - aceeasi semantica, acelasi nume
    si aceeasi signatura ca in `MenuViewModel`, fiindca template-ul de
    rezultate e comun celor doua View-uri (vezi mai jos) si se leaga de
    aceste membri indiferent care ViewModel e `DataContext`-ul curent.

- **`CartViewModel`**: cosul clientului autentificat curent.
  - `Articole` (`ObservableCollection<ArticolCosDto>`) - **nu** e o colectie
    proprie, e direct `ICartService.Articole` (aceeasi referinta) - orice
    modificare facuta din `MenuViewModel`/`SearchViewModel` se vede imediat
    aici, fara sincronizare manuala. `CosGol` (`Articole.Count == 0`) e
    folosita de View ca sa arate un mesaj cand cosul e gol.
  - `CalculCurent` (`CalculComandaDto?`) - recalculat prin
    `IOrderService.CalculeazaCostComandaAsync` de fiecare data cand
    `ICartService.CosSchimbat` se declanseaza (adaugare/stergere/schimbare
    cantitate/golire) - `null` cat timp cosul e gol.
  - `StergeArticolCommand` (parametru: `ArticolCosDto`) cheama
    `ICartService.StergeArticol`.
  - `ModificaCantitateCommand` (parametru: `ModificaCantitateParametru`, un
    `record (ArticolCosDto Articol, decimal CantitateNoua)` definit in acelasi
    fisier) cheama `ICartService.ModificaCantitate`. Parametrul compus exista
    fiindca un `ICommand` accepta un singur `CommandParameter`, iar butoanele
    "+"/"-" din `CartView` trebuie sa transmita si articolul, si cantitatea
    noua ceruta - vezi `Converters/ArticolSiDeltaCantitateConverter` mai jos.
  - `TrimiteComandaCommand` (async, dezactivata cat timp `EsteInCurs`,
    cosul e gol, sau userul nu poate comanda) cheama
    `IOrderService.CreeazaComandaAsync`; la succes goleste cosul
    (`ICartService.GolesteCos()`), seteaza `MesajSucces` cu codul comenzii si
    ridica evenimentul `ComandaCreataSucces` (cu acelasi cod), pentru un shell
    viitor care ar vrea sa navigheze automat catre `MyOrdersView`.
  - `EsteInCurs`/`MesajEroare`/`MesajSucces` pentru starea de
    incarcare/eroare/succes.

- **`MyOrdersViewModel`**: comenzile clientului autentificat curent.
  - `ToateComenzile` (`ObservableCollection<ComandaClientDto>`), populata de
    comanda `IncarcaComenziCommand` (async) din
    `IOrderService.GetComenziClientAsync`; apelata din code-behind-ul
    `MyOrdersView` la `Loaded`, la fel ca in celelalte View-uri.
  - `ComenziActive` (computed, `Where(c => c.EsteActiva)`) - cerinta
    "urmarirea comenzilor active" - si `DoarActive` (bool, bifat de un
    `CheckBox` din View) + `ComenziDeAfisat` (computed: `DoarActive ?
    ComenziActive : ToateComenzile`) - ce arata efectiv `ItemsControl`-ul.
    Toate trei sunt notificate automat prin `[NotifyPropertyChangedFor]` cand
    `ToateComenzile` sau `DoarActive` se schimba.
  - `AnuleazaComandaCommand` (parametru: `int comandaId`) cheama
    `IOrderService.AnuleazaComandaAsync`, iar la succes reincarca lista
    (`IncarcaComenziAsync()`) ca `Stare`/`EsteActiva` sa reflecte imediat
    anularea - fara dialog de confirmare (simplu, suficient pentru cerinta;
    un dialog de confirmare real ar fi un pas de UX separat).
  - `EsteInCurs`/`MesajEroare` pentru starea de incarcare/eroare.

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
- **`ArticolCosDto`**: un articol din cosul de comanda, inainte de a fi
  trimis la DB - `PreparatId`/`MeniuId` (exact unul dintre ele), `Denumire`,
  `PretUnitar`, `Cantitate`. **Observabil** (`ObservableObject` din
  CommunityToolkit.Mvvm, clasa `partial`) - singura dintre DTO-urile din
  acest folder care e observabila, fiindca e singura care se modifica dupa
  ce a fost creata (cantitatea din cos se editeaza) si trebuie sa notifice
  UI-ul (`CartView`) cand se schimba; are si o proprietate calculata
  `Subtotal` (`PretUnitar * Cantitate`), notificata automat cand
  `Cantitate` se schimba (`[NotifyPropertyChangedFor]`).
- **`CalculComandaDto`**: rezultatul lui `IOrderService.CalculeazaCostComandaAsync` -
  `SubtotalMancare`, `ProcentDiscount`/`ValoareDiscount`/`MotivDiscount`,
  `CostTransport`, `Total`, plus `AreDiscount` (computed, pentru `IsVisible`
  fara convertor).
- **`ComandaClientDto`** / **`ArticolComandaClientDto`**: rezultatul lui
  `IOrderService.GetComenziClientAsync` - o comanda cu `Articole`
  (denumire+cantitate+pret unitar), `Stare`, `CostTransport`, `Discount`,
  `Total`, `OraEstimataLivrare` si `EsteActiva`. `ArticolComandaClientDto`
  are `TextAfisare` (`"2 x Pizza Margherita"`) pentru binding direct fara
  `StringFormat` compus pe mai multe proprietati.

`OrderResult` (in `Services/`, nu in `Models/DTOs/`, la fel ca `AuthResult`)
e rezultatul lui `IOrderService.CreeazaComandaAsync`/`AnuleazaComandaAsync`:
`Succes`, `ComandaId`, `CodUnic`, `Calcul` (`CalculComandaDto?`, null pentru
anulare - nu implica niciun calcul de cost), `MesajEroare`.

## Converters noi (`Converters/`)

- **`CaleImagineToBitmapConverter`**: incarca un `Bitmap` Avalonia dintr-o
  cale de fisier (`PreparatImagine.CalePoza`); daca fisierul nu exista sau nu
  poate fi incarcat, returneaza `null` in loc sa arunce o exceptie, iar
  `MenuView` afiseaza un placeholder text ("Fara imagine") in acel caz.
- **`ArticolSiDeltaCantitateConverter`**: combina articolul din cos (valoarea
  binding-ului) cu un delta fix dat prin `ConverterParameter` (`"1"`/`"-1"`,
  scris direct in XAML) intr-un `ModificaCantitateParametru`, pentru
  `CartViewModel.ModificaCantitateCommand` - folosit de butoanele "+"/"-" din
  `CartView`, ca sa nu fie nevoie de cod in spate doar ca sa combine doua
  valori intr-un singur `CommandParameter`.

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
legaturile `IsVisible="{Binding DataContext.PoateComanda, ElementName=Root}"`
si `Command="{Binding DataContext.AdaugaInCosCommand, ElementName=Root}"`
din `MeniuAfisareItemTemplate` nu pot fi validate static la compilare pentru
ambele tipuri deodata - folosesc bindare clasica (prin reflectie), rezolvata
la runtime prin `NameScope`-ul View-ului care instantiaza template-ul
(fiecare View isi numeste radacina `x:Name="Root"`). De aceea ambele
ViewModel-uri expun o proprietate `PoateComanda` si o comanda
`AdaugaInCosCommand` cu exact acelasi nume/semnatura - contractul care face
template-ul partajabil. Butonul "Comanda" are acum `CommandParameter="{Binding}"`
(itemul insusi, `MeniuAfisareDto`), deci apasarea lui chiar adauga in cos.

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
- `CartView.axaml`(`.cs`) - un `ItemsControl` peste `Articole`, fiecare linie
  cu denumire, butoane "-"/"+" (legate de `ModificaCantitateCommand` prin
  `ArticolSiDeltaCantitateConverter`), cantitatea curenta, subtotalul liniei
  (`Subtotal`) si un buton "Sterge" (`StergeArticolCommand`). Dedesubt, un
  sumar de cost (subtotal/discount/transport/total, din `CalculCurent`,
  ascuns cat timp `CalculCurent` e `null`), mesaj de eroare/succes si butonul
  "Trimite comanda" (`TrimiteComandaCommand`). Spre deosebire de
  `MenuTemplates.axaml`, template-ul liniei din cos e definit **inline** in
  acest fisier (nu e partajat cu alt View), deci foloseste bindari compilate
  normal (fara `x:CompileBindings="False"`) pentru legaturile catre
  `ElementName=Root`. Code-behind-ul apeleaza `IncarcaCosCommand` la
  `Loaded`, ca `CalculCurent` sa fie corect chiar daca articolele au fost
  adaugate in cos inainte ca acest View sa fi fost deschis.
- `MyOrdersView.axaml`(`.cs`) - un `CheckBox` ("arata doar comenzile
  active") legat de `DoarActive`, apoi un `ItemsControl` peste
  `ComenziDeAfisat`: fiecare comanda arata codul unic, data, un badge de
  stare (verde daca `EsteActiva`, gri altfel), lista de articole
  (`TextAfisare` per articol), total, cost transport, discount (daca
  `AreDiscount`), ora estimata de livrare (daca exista) si un buton
  "Anuleaza comanda" vizibil doar cand `EsteActiva`
  (`AnuleazaComandaCommand`, cu `CommandParameter="{Binding ComandaId}"`).
  Code-behind-ul apeleaza `IncarcaComenziCommand` la `Loaded`.

## Cum se conecteaza ulterior (nu e facut inca)

Deocamdata `LoginView`/`RegisterView`/`MenuView`/`SearchView`/`CartView`/`MyOrdersView`
**nu sunt cablate in `MainWindow`** - exista independent, ca sa poata fi
verificate separat. La pasul viitor de navigare/shell:

1. `MainWindowViewModel` va tine o proprietate gen `ViewModelBase? CurrentPage`
   (sau un `ContentControl` in `MainWindow.axaml` legat de ea prin
   `ViewLocator`-ul deja existent), plus butoane/linkuri catre `SearchView`,
   `CartView` (probabil cu un contor de articole din
   `CartService.Instance.Articole.Count`) si `MyOrdersView`, vizibile din
   ecranul de meniu.
2. `MainWindowViewModel` va crea toate ViewModel-urile, se va abona la
   `LoginReusit` / `InregistrareReusita` (ca sa navigheze catre ecranul
   urmator, ex. meniul restaurantului) si la
   `NavigheazaLaInregistrareRequested` / `NavigheazaLaLoginRequested` (ca sa
   comute intre cele doua ecrane de auth), si la `CartViewModel.ComandaCreataSucces`
   (ca sa navigheze automat catre `MyOrdersView` dupa trimiterea unei
   comenzi, daca asa se decide UX-ul). `MenuView` va fi probabil ecranul
   implicit/de start, accesibil si fara autentificare.
3. Cand se introduce un container DI (sau macar o clasa de "composition
   root"), `AuthService`/`MenuService`/`OrderService`/`SessionService.Instance`/`CartService.Instance`
   se pot inregistra ca singletons si injecta explicit in ViewModels in loc
   de constructorul implicit fara parametri.
4. `SessionService.Instance.EsteAngajat` / `EsteClient` se pot folosi atunci
   in `MainWindowViewModel` ca sa decida ce ecran urmeaza dupa autentificare
   (meniu client vs. panou angajat) - fara sa se reimplementeze auth-ul sau
   afisarea meniului.
5. Cosul (`ICartService`/`CartViewModel`/`CartView`) si comenzile clientului
   (`GetComenziClientAsync`/`AnuleazaComandaAsync`/`MyOrdersViewModel`/`MyOrdersView`)
   exista deja acum (vezi sectiunile de mai sus) - butonul "Comanda" din
   `MeniuAfisareItemTemplate` chiar adauga in cos.
6. Ramane pas viitor **doar** schimbarea starii unei comenzi catre "se
   pregateste" (modulul de angajat, `feature/employee-features`). Acel cod
   trebuie sa apeleze
   `StoredProcedureRepository.UpdateCantitateTotalaLaComandaAsync(comandaId)`
   ca sa scada din stoc - nici `OrderService.CreeazaComandaAsync`, nici
   `AnuleazaComandaAsync` nu ating stocul (comanda ramane in
   `"inregistrata"` pana un angajat o trece mai departe).
