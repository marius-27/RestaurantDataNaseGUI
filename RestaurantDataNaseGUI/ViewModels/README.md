# ViewModels/ - shell-ul de navigare (MainWindowViewModel)

Pana la acest pas, fiecare ecran (Login/Register, Menu/Search, Cart/MyOrders,
si tot ce e in `ViewModels/Admin/`) exista independent, cu propriul
ViewModel + View, dar niciunul nu era cablat in `MainWindow` - aplicatia
pornea cu placeholder-ul default Avalonia ("Welcome to Avalonia!"). Acest
fisier documenteaza `MainWindowViewModel`, care leaga toate ecranele intr-o
singura aplicatie navigabila.

## Cum functioneaza navigarea

`MainWindowViewModel` expune o singura proprietate, `CurrentViewModel`
(`ViewModelBase?`), care determina ce se vede in zona de continut din
`MainWindow`. Nu exista un router/o stiva de navigare - navigarea e cat se
poate de simpla: fiecare comanda de navigare (`NavigheazaLaMeniuCommand`,
`NavigheazaLaCosCommand` etc., cate una per optiune din meniul lateral)
seteaza direct `CurrentViewModel = new XyzViewModel()`.

`MainWindow.axaml` afiseaza continutul curent printr-un simplu
`<ContentControl Content="{Binding CurrentViewModel}"/>`. `ViewLocator`
(inregistrat in `App.axaml` ca `Application.DataTemplates`) e cel care
transforma automat orice `ViewModelBase` in View-ul lui corespunzator (dupa
conventia deja existenta in proiect: namespace/sufix `ViewModel` -> acelasi
namespace/sufix `View`, ex. `ViewModels.Admin.ReportsViewModel` ->
`Views.Admin.ReportsView`) - `MainWindowViewModel` nu stie nimic despre
View-uri, doar despre ViewModel-uri.

**De ce o instanta noua la fiecare navigare, nu una cache-uita**: fiecare
View din proiect se auto-incarca la evenimentul `Loaded` din code-behind
(`viewModel.IncarcaXxxCommand.Execute(null)`, cu un flag ca sa nu se incarce
de doua ori *in aceeasi instanta*). Cream o instanta noua a ViewModel-ului
tinta la fiecare navigare, ca acest auto-load sa se declanseze din nou de
fiecare data cand utilizatorul revine la un ecran - datele afisate (meniu,
stoc, comenzi etc.) sunt intotdeauna proaspete, fara sa fie nevoie de o
comanda separata de "refresh". Starea care chiar trebuie sa persiste intre
navigari (sesiunea curenta, cosul de cumparaturi) nu sta in ViewModel-uri
oricum - vine din singleton-urile `SessionService.Instance`/
`CartService.Instance`, pe care fiecare ViewModel nou creat le citeste din
nou la construire.

## Vizibilitatea meniului lateral, in functie de rol

`MainWindowViewModel` expune trei proprietati computed peste
`ISessionService` (`EsteAutentificat`, `EsteClient`, `EsteAngajat`), pe care
`MainWindow.axaml` le foloseste direct in `IsVisible` pentru a arata/ascunde
grupuri de butoane:

| Grup | Vizibil cand | Optiuni |
|---|---|---|
| Mereu | - | Meniu Restaurant, Cautare |
| Vizitator | `!EsteAutentificat` | Login, Creeaza cont |
| Client | `EsteClient` | Cosul meu, Comenzile mele |
| Angajat | `EsteAngajat` | Categorii, Alergeni, Preparate, Meniuri, Toate comenzile, Stoc epuizare, Rapoarte |
| Autentificat | `EsteAutentificat` | Delogare |

`MainWindowViewModel` se aboneaza la `ISessionService.CurrentUserChanged` in
constructor si retransmite schimbarea prin `OnPropertyChanged` pe cele trei
proprietati computed, ca butoanele sa apara/dispara automat, fara sa fie
nevoie de vreo navigare explicita, imediat dupa login/logout.

Ca si restul ecranelor de angajat, meniul lateral e doar prima linie de
aparare - `IAdminService`/`IOrderService`/`IStockService`/`IReportService`
verifica independent `EsteAngajat` la fiecare operatie (vezi
`ViewModels/Admin/README.md`).

## Tranzitii automate

- **Dupa login reusit** (`LoginViewModel.LoginReusit`) sau **dupa
  inregistrare reusita** (`RegisterViewModel.InregistrareReusita` - userul e
  deja autentificat automat, la fel ca la login): navigheaza la "Meniu
  Restaurant", accesibil oricui.
- **Dupa delogare** (`DelogareCommand`, care apeleaza
  `ISessionService.Logout()`): navigheaza tot la "Meniu Restaurant".
- **Login <-> Register**: `LoginViewModel.NavigheazaLaInregistrareRequested`
  si `RegisterViewModel.NavigheazaLaLoginRequested` sunt evenimente pe care
  `MainWindowViewModel` le asculta pe fiecare instanta noua creata, ca sa
  poata comuta intre cele doua ecrane fara sa treaca prin Meniu.
- **Dupa trimiterea cu succes a unei comenzi**
  (`CartViewModel.ComandaCreataSucces`): **niciuna** - `MainWindowViewModel`
  nu asculta acest eveniment, deliberat, ca utilizatorul sa ramana pe Cart
  si sa vada mesajul de succes deja afisat de `CartViewModel` insusi
  (`MesajSucces`).

## Pornirea aplicatiei

`App.axaml.cs` instantiaza `new MainWindowViewModel()` (constructorul fara
parametri, care foloseste `SessionService.Instance`) ca `DataContext` al
`MainWindow` - la fel cum fiecare alt ViewModel din proiect are un
constructor implicit pentru `ViewLocator`/`Design.DataContext`. Constructorul
lui `MainWindowViewModel` apeleaza imediat `NavigheazaLaMeniu()`, deci un
vizitator neautentificat vede direct "Meniu Restaurant" la pornire, fara
niciun ecran intermediar de login obligatoriu.

## `Services/SessionService.cs` - `Logout()`

Metoda `Logout()` (goleste `CurrentUser` si declanseaza
`CurrentUserChanged`) exista deja din pasul anterior (vezi `Services/README.md`),
neschimbata - `DelogareCommand` din `MainWindowViewModel` doar o apeleaza.
