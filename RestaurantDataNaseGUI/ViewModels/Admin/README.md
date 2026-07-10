# ViewModels/Admin/ si Views/Admin/ - CRUD meniu, comenzi, stoc si rapoarte (angajat)

Tot ce e **accesibil doar utilizatorilor cu `TipUtilizator = "Angajat"`**:
CRUD complet pentru cele 4 entitati de meniu (`Services/IAdminService.cs`),
vizualizarea/urmarirea tuturor comenzilor si schimbarea starii lor
(`Services/IOrderService.cs`, extins - vezi mai jos), vizualizarea
stocului aproape de epuizare (`Services/IStockService.cs`) si generarea de
rapoarte (`Services/IReportService.cs` - vezi mai jos). Toate verifica
autorizarea la nivel de service, nu doar in UI.

## `Services/IAdminService.cs` / `AdminService.cs`

O singura clasa cu operatii CRUD pentru toate cele 4 entitati (`Categorie`,
`Alergen`, `Preparat`, `Meniu`), toate prin EF Core LINQ (parametrizat
automat de provider - fara SQL brut).

### Autorizare

**Fiecare** metoda de Create/Update/Delete incepe cu `VerificaEsteAngajat()`
(metoda privata): daca `ISessionService.EsteAutentificat` e `false` sau
`EsteAngajat` e `false`, metoda returneaza imediat
`AdminResult.Esec("Aceasta actiune este permisa doar angajatilor
autentificati.")`, fara sa atinga baza de date. Metodele de citire
(`GetCategoriiAsync` etc.) nu verifica asta - un shell viitor va decide daca
arata ecranele de administrare (vezi `PoateAdministra` mai jos), dar chiar
si asa, mutatiile raman protejate la nivel de service, nu doar de UI.

`AdminResult` (`Succes`, `MesajEroare?`) e folosit pentru toate rezultatele
de Create/Update/Delete, la fel ca `AuthResult`/`OrderResult` din celelalte
servicii - evita exceptii pentru cazurile "asteptate" (neautorizat, denumire
duplicata, entitate folosita in alta parte).

### Categorie / Alergen

CRUD simplu, simetric intre cele doua: `Get.../CreeazaX/ActualizeazaX/StergeX`.
Denumirea trebuie sa fie unica (verificata cu `AnyAsync` inainte de insert/
update, plus un fallback pe `DbUpdateException`/`SqlException` 2601/2627
pentru cursa dintre verificare si scriere - acelasi tipar ca in
`AuthService`).

- `StergeCategorieAsync` blocheaza stergerea daca exista `Preparate` sau
  `Meniuri` cu acea `CategorieId` ("Categoria are preparate sau meniuri
  asociate si nu poate fi stearsa.").
- `StergeAlergenAsync` blocheaza stergerea daca exista randuri in
  `PreparatAlergen` pentru acel alergen ("Alergenul este asociat unor
  preparate si nu poate fi sters.").

### Preparat

`GetPreparateAsync` include `Categorie`, `PreparatAlergeni.Alergen` si
`Imagini` pentru fiecare preparat (afisare completa fara N+1 queries).

`CreeazaPreparatAsync`/`ActualizeazaPreparatAsync` primesc un
`PreparatFormDto` (campurile scalare + `List<int> AlergenIds` +
`List<string> ImaginiPaths`). La actualizare, listele de alergeni si
imagini sunt **inlocuite integral**: se sterg toate `PreparatAlergen`/
`PreparatImagine` existente ale preparatului (`RemoveRange`) si se re-adauga
din lista noua trimisa de formular - mai simplu si mai putin predispus la
erori decat un diff (adaugate/sterse/neschimbate) fata de starea anterioara,
si suficient de eficient pentru cate randuri are de regula un preparat.

`StergePreparatAsync` - regula ceruta explicit, verificata in ordine:
1. Daca preparatul **a fost deja folosit intr-o comanda** (exista randuri in
   `ComandaDetaliu` cu acel `PreparatId`) - **nu se sterge fizic**. Se
   apeleaza `StoredProcedureRepository.SetPreparatIndisponibilAsync`
   (`dbo.sp_SetPreparatIndisponibil`), soft-delete conform conventiei deja
   documentate in `database/README.md`.
2. Altfel, daca preparatul **face parte dintr-un meniu** (exista randuri in
   `MeniuPreparat`) - stergerea e blocata cu un mesaj clar ("... nu poate fi
   sters. Scoate-l mai intai din meniuri."). FK-ul `FK_MeniuPreparat_Preparat`
   e `Restrict`, deci un `DELETE` ar fi esuat oricum cu o eroare SQL bruta;
   verificarea explicita da un mesaj util in loc de atat.
3. Altfel, `DELETE` fizic normal.

### Meniu

`GetMeniuriAsync` include `Categorie` si `MeniuPreparate.Preparat`
(componentele, cu `CantitateInMeniu` si denumirea preparatului fiecarei
componente).

`CreeazaMeniuAsync`/`ActualizeazaMeniuAsync` primesc un `MeniuFormDto`
(campurile scalare + `List<MeniuPreparatFormDto>` cu perechi
`PreparatId`+`CantitateInMeniu`). La actualizare, lista de componente e
**inlocuita integral**, la fel ca la `Preparat`.

`StergeMeniuAsync` - **schema nu are o coloana `Disponibil` pentru Meniu**
(vezi `database/README.md`: disponibilitatea unui meniu se deriva din
disponibilitatea preparatelor lui, nu dintr-o coloana proprie). De aceea,
varianta aleasa e cea mai simpla dintre cele sugerate: daca meniul a fost
folosit in comenzi (exista randuri in `ComandaDetaliu` cu acel `MeniuId`),
stergerea e **blocata cu un mesaj clar** ("Meniul a fost folosit in comenzi
si nu poate fi sters.") - fara soft-delete dedicat pentru `Meniu`. Altfel,
`DELETE` fizic normal.

## `Services/IOrderService.cs` / `OrderService.cs` (extins) - partea de angajat

`OrderService` avea deja partea de client (creare/anulare comenzi - vezi
sectiunile anterioare din acest README, sau istoricul lui). Aceasta
extindere adauga partea de angajat, **in aceeasi clasa** (nu un serviciu
nou), fiindca tine tot de comenzi si de aceleasi stari (`StariFinale`, deja
existent).

### Vizualizare comenzi

- **`GetToateComenzileAsync()`** - toate comenzile, ale tuturor clientilor,
  sortate descrescator dupa `DataComanda`. Interogare EF Core LINQ cu
  `Include(c => c.Utilizator)`, `Include(c => c.Stare)` si
  `Include(c => c.ComandaDetalii).ThenInclude(cd => cd.Preparat/.Meniu)` -
  **nu** foloseste `sp_GetComenziClientCuDetalii` (acea procedura e
  filtrata pe un singur `UtilizatorId`, nepotrivita pentru "toate comenzile,
  ai tuturor clientilor"). Mapeaza fiecare `Comanda` intr-un
  `ComandaAngajatDto` (vezi mai jos), cu subtotal/total recalculate din
  liniile de comanda si `Nume`/`Prenume`/`Telefon`/`AdresaLivrare` preluate
  din `Comanda.Utilizator`.
- **`GetComenziActiveAngajatAsync()`** - acelasi rezultat, filtrat in
  memorie pe `EsteActiva` (nelivrate, neanulate).
- Ambele **arunca `UnauthorizedAccessException`** daca userul curent nu e un
  angajat autentificat (`VerificaEsteAngajatSauArunca`, metoda privata) - nu
  returneaza un `Result`, fiindca metodele de citire din acest serviciu
  (`GetComenziClientAsync` etc.) nu au avut niciodata un asemenea wrapper;
  exceptia e prinsa in ViewModel si transformata in `MesajEroare`.

### Schimbarea starii unei comenzi

**Tranzitii valide** (dictionarul static `TranzitiiValide`,
case-insensitive):

| Din starea... | ...se poate trece in |
|---|---|
| `inregistrata` | `se pregateste`, `anulata` |
| `se pregateste` | `a plecat la client`, `anulata` |
| `a plecat la client` | `livrata`, `anulata` |
| `livrata` / `anulata` | (nicio tranzitie - stari finale) |

Nu exista nicio alta tranzitie (ex. direct din `inregistrata` in `livrata`).
`GetStariUrmatoarePosibile(stareCurenta)` expune aceasta lista pentru UI
(dropdown-ul din `ToateComenzileView` arata doar starile chiar valide pentru
comanda respectiva).

**`SchimbaStareComandaAsync(comandaId, stareNoua)`**:
1. Verifica `EsteAngajat` (returneaza `OrderResult.Esec`, nu arunca -
   spre deosebire de `GetToateComenzileAsync`, asta e o mutatie, la fel ca
   `CreeazaComandaAsync`/`AnuleazaComandaAsync`, deci foloseste acelasi
   tipar `OrderResult` in loc de exceptie).
2. Respinge daca starea curenta e deja finala, sau daca `stareNoua` nu e in
   `GetStariUrmatoarePosibile(stareCurenta)`.
3. **Daca `stareNoua` e `"se pregateste"`**: cerinta explicita din tema -
   "la fiecare comanda pusa in pregatire se actualizeaza automat cantitatea
   totala din restaurant". Actualizarea `StareId` si apelul catre
   `StoredProcedureRepository.UpdateCantitateTotalaLaComandaAsync` se fac
   **in aceeasi tranzactie** (`context.Database.BeginTransactionAsync`):
   - `sp_UpdateCantitateTotalaLaComanda` are propriul `BEGIN
     TRANSACTION`/`ROLLBACK TRANSACTION` intern (vezi `database/schema.sql`)
     si arunca o eroare SQL (`RAISERROR`) daca stocul ar deveni negativ
     pentru vreun preparat.
   - Acea eroare devine o `SqlException` in .NET, prinsa de `catch` -
     schimbarea de stare deja salvata e anulata (`transaction.RollbackAsync`)
     si se returneaza `OrderResult.Esec(...)` cu un mesaj clar ("probabil
     din cauza stocului insuficient... comanda a ramas in starea
     anterioara").
   - Rollback-ul din C# e el insusi infasurat intr-un `try/catch` care
     ignora o eventuala eroare "tranzactia s-a incheiat deja" - fiindca
     `ROLLBACK TRANSACTION` din procedura poate incheia tranzactia deja la
     nivel de server inainte sa ajunga controlul inapoi in C#; rezultatul
     dorit (nimic din schimbarea asta nu ramane salvat) e oricum garantat
     de `ROLLBACK`-ul din procedura, indiferent daca al doilea rollback de
     pe partea de client reuseste sau nu.
4. Pentru orice alta stare noua (ex. `"a plecat la client"`, `"livrata"`,
   `"anulata"`), doar `StareId` se actualizeaza - fara apel catre stoc.

## `Services/IStockService.cs` / `StockService.cs`

Separat de `AdminService` (nu e CRUD, doar citire) si de `OrderService`.
`GetPreparateApropiateDeEpuizareAsync()` - doar angajati autentificati
(arunca `UnauthorizedAccessException` altfel, la fel ca `GetToateComenzileAsync`),
apeleaza `StoredProcedureRepository.GetPreparateApropiateDeEpuizareAsync(pragCantitate: null, ...)`
- **fara** parametru explicit de prag, ca procedura sa foloseasca implicit
cheia `PragStocEpuizare` din `dbo.Configurare`.

## `Services/IReportService.cs` / `ReportService.cs`

Cerinta din tema ("Se vor putea genera rapoarte de catre angajatii
restaurantului") nu specifica ce rapoarte anume - alegerea de mai jos
acopera cele patru intrebari pe care le pune de regula cineva care conduce
un restaurant: *cat s-a vandut si cand?*, *ce se vinde cel mai bine?*, *ce
categorii aduc banii?* si *ce mai e pe stoc?*. Toate patru sunt **doar
citire**, verifica `EsteAngajat` la fel ca `GetToateComenzileAsync`
(arunca `UnauthorizedAccessException`, nu returneaza un `Result` - acelasi
motiv: sunt metode de citire, nu mutatii), si folosesc **exclusiv EF Core
LINQ** (fara SQL brut, fara proceduri stocate noi) - agregarile (`Sum`,
`Count`, `GroupBy`) sunt parametrizate automat de provider.

Tipar comun de interogare: filtrarea pe interval de date si sumele
per-comanda/per-linie (proiectate printr-un `Select` cu un `Sum` corelat pe
`ComandaDetalii`) sunt translatate de EF Core in SQL parametrizat; gruparea
finala (pe zi/preparat/categorie) si aplicarea discountului (care foloseste
`Math.Round`, netranslatabil in SQL) se fac cu LINQ in memorie, dupa
incarcare - acelasi tipar deja folosit in
`OrderService.GetComenziClientAsync`/`MapeazaComandaAngajat`. Filtrarea pe
starea "anulata" foloseste tot in-memory un `HashSet<string>` cu comparator
`OrdinalIgnoreCase` (`StariAnulate`), niciodata direct intr-un `Where` LINQ
- daca ar fi translatat intr-un `IN (...)` SQL, semantica *case-insensitive*
nu ar mai fi garantata (colatia bazei de date poate diferi).

- **`RaportVanzariPerioadaAsync(dataStart, dataEnd)`**: numarul de comenzi
  plasate in interval (toate starile), cate dintre ele au fost anulate si
  suma totala incasata (mancare + transport - discount, comenzile anulate
  excluse - discountul e aplicat corect, per comanda, la fel ca in
  `OrderService`), plus o defalcare pe zi (`Zile: List<VanzareZilnicaDto>`).
- **`RaportPreparateCelMaiVanduteAsync(dataStart, dataEnd, top = 10)`**: top
  preparate/meniuri dupa cantitatea totala comandata in interval, prin
  `ComandaDetalii` (comenzile anulate sunt excluse). Fiindca un rand din
  `ComandaDetaliu` e fie un `Preparat`, fie un `Meniu` (niciodata ambele -
  vezi CHECK constraint din schema), gruparea se face pe perechea
  `(PreparatId, MeniuId)`, iar denumirea/categoria se preiau din oricare
  dintre cele doua navigatii e nenula.
- **`RaportVanzariPeCategorieAsync(dataStart, dataEnd)`**: suma vanzarilor
  (comenzile anulate excluse) grupata pe categoria preparatului/meniului
  fiecarei linii de comanda.
- **`RaportStocCurentAsync()`**: toate preparatele (indiferent de
  `Disponibil`), sortate pe categorie apoi denumire - stocul curent
  (`CantitateTotalaRestaurant` + `UnitateMasura`) al fiecaruia. Nu reuseste
  logica din `StockService` (aceea foloseste `sp_GetPreparateApropiateDeEpuizare`,
  o procedura stocata filtrata pe un prag; aici se cere un inventar complet,
  pur LINQ, deci o interogare noua peste `context.Preparate`).

**Simplificare asumata**: `RaportPreparateCelMaiVanduteAsync` si
`RaportVanzariPeCategorieAsync` raporteaza suma *bruta* a liniilor
(`Cantitate * PretUnitarLaComanda`), **fara** sa proratizeze discountul
aplicat la nivelul intregii comenzi pe fiecare linie/categorie in parte -
alocarea proportionala a unui discount procentual pe linii ar complica
raportul fara sa schimbe concluzia lui (ce se vinde cel mai bine / ce
categorii aduc cei mai multi bani), iar `RaportVanzariPerioadaAsync` (unde
suma totala *chiar* conteaza) aplica deja discountul corect, per comanda.

## `Models/DTOs/` noi

- **`ComandaAngajatDto`**: **mosteneste** `ComandaClientDto` (nu il
  duplica) si adauga `NumeClient`/`PrenumeClient`/`TelefonClient`/`AdresaLivrareClient`
  - toate campurile cerute explicit pentru vizualizarea unei comenzi de
    catre un angajat (data, cod, articole cu cantitati, cost mancare/
    transport/total, ora estimata livrare, stare, plus datele clientului).
- **`CategorieFormDto`** / **`AlergenFormDto`**: `Id` (0 = nou) + `Denumire`.
- **`PreparatFormDto`**: campurile scalare ale `Preparat` (inclusiv
  `Disponibil`, editabil manual de un angajat - independent de soft-delete-ul
  automat de la stergere) + `AlergenIds` + `ImaginiPaths`.
- **`MeniuFormDto`** / **`MeniuPreparatFormDto`**: campurile scalare ale
  `Meniu` + `Preparate` (lista de `PreparatId`+`CantitateInMeniu`).

Toate patru sunt DTO-uri "plate", fara logica - conversia spre/dinspre
formularul din ViewModel se face in ViewModels, nu in DTO.

## `Models/DTOs/Reports/` (rezultatele `IReportService`)

- **`RaportVanzariDto`** (+ **`VanzareZilnicaDto`** pentru fiecare zi din
  `Zile`): rezultatul `RaportVanzariPerioadaAsync`.
- **`PreparatVandutDto`**: o linie din `RaportPreparateCelMaiVanduteAsync`
  (`Tip` = `"Preparat"` sau `"Meniu"`).
- **`VanzareCategorieDto`**: o linie din `RaportVanzariPeCategorieAsync`.
- **`PreparatStocDto`**: o linie din `RaportStocCurentAsync`.

Toate patru sunt DTO-uri "plate" fara logica, la fel ca restul - populate
direct prin proiectii `Select` in `ReportService`.

## ViewModels (`ViewModels/Admin/`)

Patru ViewModel-uri, cate unul per entitate, toate cu aceeasi forma
generala (liste + formular + `Salveaza`/`Sterge`/`Anuleaza` + `EsteInCurs`/
`MesajEroare`):

- **`CategorieAdminViewModel`** / **`AlergenAdminViewModel`**: cele mai
  simple - o `ObservableCollection<Categorie>`/`ObservableCollection<Alergen>`,
  un camp `Denumire`, `IdInEditare` (0 = formular de creare, altfel editare -
  proprietatea computata `EsteEditare` e notificata automat prin
  `[NotifyPropertyChangedFor]`). `SelecteazaPentruEditareCommand` populeaza
  formularul dintr-un item din lista; `SalveazaCommand` decide Create vs.
  Update dupa `EsteEditare`; `AnuleazaCommand` goleste formularul.
- **`PreparatAdminViewModel`**: acelasi tipar, plus:
  - `CategoriiDisponibile` (pentru un `ComboBox`) si `AlergeniSelectabili`
    (`ObservableCollection<AlergenSelectabilViewModel>` - cate un item per
    alergen din DB, cu o proprietate `EsteSelectat` bindabila direct la un
    `CheckBox`; la editare, `SelecteazaPentruEditare` bifeaza alergenii deja
    asociati preparatului selectat).
  - `ImaginiPaths` (`ObservableCollection<string>`) - lista editabila:
    `AdaugaImagineCommand` adauga `CaleImagineNoua` (un `TextBox` separat) in
    lista, `StergeImagineCommand` (parametru: calea) sterge un rand.
  - La salvare, construieste `PreparatFormDto.AlergenIds` din
    `AlergeniSelectabili.Where(a => a.EsteSelectat)` si
    `PreparatFormDto.ImaginiPaths` direct din `ImaginiPaths`.
- **`MeniuAdminViewModel`**: acelasi tipar, plus:
  - `PreparateDisponibile` (pentru un `ComboBox`) si `Componente`
    (`ObservableCollection<MeniuComponentaViewModel>` - preparat + cantitate,
    editabila): `PreparatDeAdaugat` + `CantitateDeAdaugat` sunt campurile
    unui rand nou; `AdaugaComponentaCommand` il adauga in `Componente` (cu
    validare: preparat selectat, cantitate pozitiva, nu e deja adaugat),
    `StergeComponentaCommand` (parametru: componenta) sterge un rand.
  - La salvare, construieste `MeniuFormDto.Preparate` direct din
    `Componente`.

Toate patru au un constructor implicit (folosit de `Design.DataContext` din
XAML si de `ViewLocator`) care instantiaza direct `AdminService` +
`SessionService.Instance`, si un al doilea constructor care accepta
`IAdminService`/`ISessionService` explicit (teste / injectie ulterioara) -
acelasi tipar ca `LoginViewModel`/`MenuViewModel` etc. Toate expun si
`PoateAdministra` (`ISessionService.EsteAngajat`), notificat la
`CurrentUserChanged`, desi ecranele insele nu se ascund singure - un shell
viitor il poate folosi ca sa decida daca arata deloc aceste View-uri (vezi
mai jos).

- **`ToateComenzileViewModel`**: `Comenzi`
  (`ObservableCollection<ComandaAngajatRandViewModel>`), `DoarActive` (toggle,
  la fel ca `MyOrdersViewModel.DoarActive` de pe partea de client) +
  `ComenziDeAfisat` (computed, notificat automat prin `[NotifyPropertyChangedFor]`
  cand `Comenzi`/`DoarActive` se schimba). `IncarcaComenziCommand` populeaza
  `Comenzi` din `IOrderService.GetToateComenzileAsync()`, impachetand fiecare
  `ComandaAngajatDto` intr-un `ComandaAngajatRandViewModel` cu starile lui
  urmatoare posibile deja calculate (`GetStariUrmatoarePosibile`).
  `SchimbaStareCommand` (parametru: `ComandaAngajatRandViewModel` - poarta si
  `ComandaId`, si `StareSelectata` intr-un singur obiect, la fel ca
  `ModificaCantitateParametru` din cosul de client) cheama
  `IOrderService.SchimbaStareComandaAsync` si reincarca lista la succes.
- **`ComandaAngajatRandViewModel`**: `Comanda` (`ComandaAngajatDto`) +
  `StariDisponibile` (`ObservableCollection<string>`, starile urmatoare
  valide pentru aceasta comanda) + `StareSelectata` (bindabil la un
  `ComboBox`) - un rand din `ToateComenzileView`.
- **`StocEpuizareViewModel`**: `Preparate`
  (`ObservableCollection<PreparatEpuizareDto>`) + `NuAreRezultate` (computed,
  pentru mesajul "niciun preparat aproape de epuizare"), populate de
  `IncarcaStocCommand` din `IStockService.GetPreparateApropiateDeEpuizareAsync()`.
- **`ReportsViewModel`**: `TipuriRaport` (`ObservableCollection<TipRaportOptiune>`,
  cate o pereche `TipRaport`+denumire afisata per raport din `IReportService`)
  + `TipSelectat` (bindabil la un `ComboBox`) + `DataStart`/`DataEnd`
  (`DateTimeOffset?`, implicit ultimele 30 de zile, bindabile direct la
  `DatePicker.SelectedDate`) + `Top` (doar pentru raportul de preparate).
  Rezultatul **nu** e o singura colectie cu "coloane dinamice", ci patru
  proprietati separate, cate una per tip de raport (`RaportVanzari`,
  `PreparateVandute`, `VanzariPeCategorie`, `StocCurent`) - fiecare raport
  are propriile campuri, deci patru `DataTemplate`-uri tipizate sunt mai
  simplu de legat in Avalonia decat un model generic de randuri/coloane.
  `AfiseazaVanzari`/`AfiseazaPreparate`/`AfiseazaCategorii`/`AfiseazaStoc`
  (computed, notificate la schimbarea lui `TipSelectat`) decid ce sectiune
  a `ReportsView` e vizibila. `GenereazaRaportCommand` apeleaza metoda
  potrivita din `IReportService` dupa `TipSelectat.Tip`; spre deosebire de
  `StocEpuizareView`/`ToateComenzileView`, **nu** se incarca automat la
  `Loaded` - un raport are parametri (interval, top) pe care userul trebuie
  sa-i aleaga intai, deci generarea e mereu un gest explicit ("Genereaza").
  `ExportaCsvCommand` scrie randurile raportului curent afisat
  intr-un fisier `.csv` in `Rapoarte/` (relativ la directorul aplicatiei),
  cu escaping minim (ghilimele in jurul valorilor ce contin virgula/ghilimele/
  linie noua) - fara nicio libraie externa de CSV, doar concatenare de
  string-uri, cerinta explicita din tema pentru acest export.

### Clase mici auxiliare

- **`AlergenSelectabilViewModel`**: `AlergenId` + `Denumire` + `EsteSelectat`
  (bindabil) - un rand din lista de CheckBox-uri din `PreparatAdminViewModel`.
- **`MeniuComponentaViewModel`**: `PreparatId` + `Denumire` + `CantitateInMeniu`
  (bindabil) - un rand din lista editabila de componente din
  `MeniuAdminViewModel`.
- **`ComandaAngajatRandViewModel`**: vezi mai sus.
- **`TipRaportOptiune`** (`ViewModels/Admin/TipRaport.cs`): `Tip`
  (`TipRaport`, enum) + `Denumire` (text afisat) - un item din `ComboBox`-ul
  de tip raport din `ReportsViewModel`.

## Views (`Views/Admin/`)

Cate un `UserControl` per ViewModel (`CategorieAdminView`, `AlergenAdminView`,
`PreparatAdminView`, `MeniuAdminView`, `ToateComenzileView`, `StocEpuizareView`,
`ReportsView`), fara logica in code-behind (doar apelul `IncarcaXxxCommand` la
evenimentul `Loaded`, la fel ca in restul proiectului - cu exceptia
`ReportsView`, vezi mai sus de ce). Ecranele CRUD folosesc acelasi
tipar de layout: un formular sus/in stanga (creare sau editare, dupa
`EsteEditare`) si lista existentelor dedesubt/in dreapta, cu butoane
"Editeaza"/"Sterge" pe fiecare rand (legate de
`DataContext.XxxCommand, ElementName=Root` din template-ul randului, catre
`SelecteazaPentruEditareCommand`/`StergeCommand` ale ViewModel-ului
radacina).

`CategorieAdminView`/`AlergenAdminView` sunt aproape identice (o singura
`TextBox` + lista). `PreparatAdminView`/`MeniuAdminView` au formulare mai
bogate (`NumericUpDown` pentru valori zecimale, `ComboBox` cu `ItemTemplate`
pentru `Categorie`/`Preparat`, liste editabile pentru alergeni/imagini/
componente).

`ToateComenzileView` arata, per comanda: cod, data, badge de stare (verde
daca activa, gri altfel), datele clientului (nume, prenume, telefon, adresa
de livrare), lista de articole cu cantitati, costul mancarii/transportului/
discountului/totalului, ora estimata de livrare si, doar pentru comenzile
active, un `ComboBox` cu starile urmatoare valide + butonul "Confirma"
(`SchimbaStareCommand`). `StocEpuizareView` e o simpla lista de preparate
(denumire, categorie, cantitate + unitate de masura, "Indisponibil" daca e
cazul).

`ReportsView` are trei zone: (1) formularul de parametri (selector tip
raport, `DatePicker` de la/pana la - ascuns pentru "Stoc curent", care nu
are interval -, `NumericUpDown` "Top" - vizibil doar pentru raportul de
preparate -, butoanele "Genereaza"/"Exporta CSV"); (2) un rezumat text
vizibil doar pentru raportul de vanzari (numar comenzi, comenzi anulate,
suma totala); (3) zona de rezultate cu patru `ItemsControl`-uri, cate unul
per tip de raport, cu `IsVisible` legat de `AfiseazaVanzari`/`AfiseazaPreparate`/
`AfiseazaCategorii`/`AfiseazaStoc` - doar unul e vizibil o data, in functie
de `TipSelectat`.

## Cum e conectat in shell

Ecranele de administrare sunt acum cablate in `MainWindow` prin
`MainWindowViewModel` (vezi `ViewModels/README.md`):

1. `MainWindowViewModel` afiseaza meniul de administrare (Categorii /
   Alergeni / Preparate / Meniuri / Toate comenzile / Stoc epuizare /
   Rapoarte) **doar** cand `EsteAngajat` e `true` - o proprietate computed
   peste `ISessionService`, notificata la `CurrentUserChanged`, echivalenta
   cu `PoateAdministra` expus de fiecare ViewModel de aici.
2. Chiar daca acel meniu ar fi ascuns dintr-o eroare de navigare, mutatiile/
   citirile raman sigure: `IAdminService`/`IOrderService`/`IStockService`/
   `IReportService` verifica independent `EsteAngajat` la fiecare operatie
   relevanta, deci UI-ul nu e singura linie de aparare.
