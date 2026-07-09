# ViewModels/Admin/ si Views/Admin/ - CRUD Categorii/Alergeni/Preparate/Meniuri

CRUD complet pentru cele 4 entitati de meniu, **accesibil doar
utilizatorilor cu `TipUtilizator = "Angajat"`** (verificat de
`Services/IAdminService.cs`/`AdminService.cs`, nu doar in UI). **Nu contine
vizualizarea comenzilor si nici schimbarea starilor lor** - asta e un modul
separat, viitor.

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

## `Models/DTOs/` noi

- **`CategorieFormDto`** / **`AlergenFormDto`**: `Id` (0 = nou) + `Denumire`.
- **`PreparatFormDto`**: campurile scalare ale `Preparat` (inclusiv
  `Disponibil`, editabil manual de un angajat - independent de soft-delete-ul
  automat de la stergere) + `AlergenIds` + `ImaginiPaths`.
- **`MeniuFormDto`** / **`MeniuPreparatFormDto`**: campurile scalare ale
  `Meniu` + `Preparate` (lista de `PreparatId`+`CantitateInMeniu`).

Toate patru sunt DTO-uri "plate", fara logica - conversia spre/dinspre
formularul din ViewModel se face in ViewModels, nu in DTO.

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

### Clase mici auxiliare

- **`AlergenSelectabilViewModel`**: `AlergenId` + `Denumire` + `EsteSelectat`
  (bindabil) - un rand din lista de CheckBox-uri din `PreparatAdminViewModel`.
- **`MeniuComponentaViewModel`**: `PreparatId` + `Denumire` + `CantitateInMeniu`
  (bindabil) - un rand din lista editabila de componente din
  `MeniuAdminViewModel`.

## Views (`Views/Admin/`)

Cate un `UserControl` per ViewModel (`CategorieAdminView`, `AlergenAdminView`,
`PreparatAdminView`, `MeniuAdminView`), fara logica in code-behind (doar
apelul `IncarcaCommand` la evenimentul `Loaded`, la fel ca in restul
proiectului). Toate folosesc acelasi tipar de layout: un formular sus/in
stanga (creare sau editare, dupa `EsteEditare`) si lista existentelor
dedesubt/in dreapta, cu butoane "Editeaza"/"Sterge" pe fiecare rand
(legate de `DataContext.XxxCommand, ElementName=Root` din template-ul
randului, catre `SelecteazaPentruEditareCommand`/`StergeCommand` ale
ViewModel-ului radacina).

`CategorieAdminView`/`AlergenAdminView` sunt aproape identice (o singura
`TextBox` + lista). `PreparatAdminView`/`MeniuAdminView` au formulare mai
bogate (`NumericUpDown` pentru valori zecimale, `ComboBox` cu `ItemTemplate`
pentru `Categorie`/`Preparat`, liste editabile pentru alergeni/imagini/
componente).

## Cum se conecteaza ulterior (nu e facut inca)

Ca si restul View-urilor din proiect, cele 4 ecrane de administrare **nu
sunt cablate in `MainWindow`** - exista independent. La pasul viitor de
navigare/shell:

1. `MainWindowViewModel` va afisa un meniu de administrare (Categorii /
   Alergeni / Preparate / Meniuri) **doar** cand
   `SessionService.Instance.EsteAngajat` e `true` - exact ca `PoateAdministra`
   expus de fiecare ViewModel de aici.
2. Chiar daca acel meniu ar fi ascuns dintr-o eroare de navigare, mutatiile
   raman sigure: `IAdminService` verifica independent `EsteAngajat` la
   fiecare Create/Update/Delete, deci UI-ul nu e singura linie de aparare.
3. Modulul urmator (vizualizarea comenzilor si schimbarea starilor lor de
   catre angajati, inclusiv apelul catre
   `StoredProcedureRepository.UpdateCantitateTotalaLaComandaAsync` cand o
   comanda trece in "se pregateste") va fi un set separat de
   Service/ViewModel/View, fara sa modifice ce exista deja aici.
