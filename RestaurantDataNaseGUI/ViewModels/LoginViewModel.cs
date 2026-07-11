using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _parola = string.Empty;

    [ObservableProperty]
    private string? _mesajEroare;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool _esteInCurs;

    // Declansat dupa login reusit, pentru navigare intr-un shell viitor.
    public event EventHandler<Utilizator>? LoginReusit;

    // Cerere de comutare la ecranul de inregistrare.
    public event EventHandler? NavigheazaLaInregistrareRequested;

    public LoginViewModel() : this(new AuthService(), SessionService.Instance)
    {
    }

    public LoginViewModel(IAuthService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    private bool PoateAutentifica() => !EsteInCurs;

    [RelayCommand(CanExecute = nameof(PoateAutentifica))]
    private async Task LoginAsync()
    {
        MesajEroare = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Parola))
        {
            MesajEroare = "Email si parola sunt obligatorii.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var rezultat = await _authService.LoginAsync(Email, Parola);

            if (!rezultat.Succes || rezultat.Utilizator is null)
            {
                MesajEroare = rezultat.MesajEroare ?? "Autentificare esuata.";
                return;
            }

            _sessionService.SetCurrentUser(rezultat.Utilizator);
            LoginReusit?.Invoke(this, rezultat.Utilizator);
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private void NavigheazaLaInregistrare()
    {
        NavigheazaLaInregistrareRequested?.Invoke(this, EventArgs.Empty);
    }
}
