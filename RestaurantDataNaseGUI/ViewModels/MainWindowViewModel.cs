using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Services;
using RestaurantDataNaseGUI.ViewModels.Admin;

namespace RestaurantDataNaseGUI.ViewModels;

// Shell-ul aplicatiei: tine CurrentViewModel (rezolvat prin ViewLocator) si
// comenzile de navigare din meniul lateral; fiecare comanda creeaza o
// instanta noua a ViewModel-ului tinta, deci revenirea la un ecran ii reia
// datele de la zero. Vizibilitatea optiunilor de meniu e legata de
// EsteAutentificat/EsteClient/EsteAngajat, notificate la CurrentUserChanged.
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private ViewModelBase? _currentViewModel;

    public bool EsteAutentificat => _sessionService.EsteAutentificat;
    public bool EsteClient => _sessionService.EsteClient;
    public bool EsteAngajat => _sessionService.EsteAngajat;

    public MainWindowViewModel() : this(SessionService.Instance)
    {
    }

    public MainWindowViewModel(ISessionService sessionService)
    {
        _sessionService = sessionService;
        _sessionService.CurrentUserChanged += OnCurrentUserChanged;

        NavigheazaLaMeniu();
    }

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(EsteAutentificat));
        OnPropertyChanged(nameof(EsteClient));
        OnPropertyChanged(nameof(EsteAngajat));
    }

    [RelayCommand]
    private void NavigheazaLaMeniu() => CurrentViewModel = new MenuViewModel();

    [RelayCommand]
    private void NavigheazaLaCautare() => CurrentViewModel = new SearchViewModel();

    [RelayCommand]
    private void NavigheazaLaCos() => CurrentViewModel = new CartViewModel();

    [RelayCommand]
    private void NavigheazaLaComenzileMele() => CurrentViewModel = new MyOrdersViewModel();

    [RelayCommand]
    private void NavigheazaLaCategorii() => CurrentViewModel = new CategorieAdminViewModel();

    [RelayCommand]
    private void NavigheazaLaAlergeni() => CurrentViewModel = new AlergenAdminViewModel();

    [RelayCommand]
    private void NavigheazaLaPreparate() => CurrentViewModel = new PreparatAdminViewModel();

    [RelayCommand]
    private void NavigheazaLaMeniuri() => CurrentViewModel = new MeniuAdminViewModel();

    [RelayCommand]
    private void NavigheazaLaToateComenzile() => CurrentViewModel = new ToateComenzileViewModel();

    [RelayCommand]
    private void NavigheazaLaStocEpuizare() => CurrentViewModel = new StocEpuizareViewModel();

    [RelayCommand]
    private void NavigheazaLaRapoarte() => CurrentViewModel = new ReportsViewModel();

    [RelayCommand]
    private void NavigheazaLaLogin() => AfiseazaLogin();

    [RelayCommand]
    private void NavigheazaLaInregistrare() => AfiseazaInregistrare();

    [RelayCommand]
    private void Delogare()
    {
        _sessionService.Logout();
        NavigheazaLaMeniu();
    }

    private void AfiseazaLogin()
    {
        var viewModel = new LoginViewModel();
        viewModel.LoginReusit += (_, _) => NavigheazaLaMeniu();
        viewModel.NavigheazaLaInregistrareRequested += (_, _) => AfiseazaInregistrare();
        CurrentViewModel = viewModel;
    }

    private void AfiseazaInregistrare()
    {
        var viewModel = new RegisterViewModel();
        viewModel.InregistrareReusita += (_, _) => NavigheazaLaMeniu();
        viewModel.NavigheazaLaLoginRequested += (_, _) => AfiseazaLogin();
        CurrentViewModel = viewModel;
    }
}
