using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

public partial class RegisterViewModel : ViewModelBase
{
    private const int LungimeMinimaParola = 8;

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TelefonRegex = new(
        @"^[0-9+()\-\s]{7,20}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAuthService _authService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private string _nume = string.Empty;

    [ObservableProperty]
    private string _prenume = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _telefon = string.Empty;

    [ObservableProperty]
    private string _adresaLivrare = string.Empty;

    [ObservableProperty]
    private string _parola = string.Empty;

    [ObservableProperty]
    private string _confirmareParola = string.Empty;

    [ObservableProperty]
    private string? _mesajEroare;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool _esteInCurs;

    /// <summary>Se declanseaza dupa o inregistrare reusita (utilizatorul e deja autentificat).</summary>
    public event EventHandler<Utilizator>? InregistrareReusita;

    /// <summary>Cerere de comutare catre ecranul de login.</summary>
    public event EventHandler? NavigheazaLaLoginRequested;

    public RegisterViewModel() : this(new AuthService(), SessionService.Instance)
    {
    }

    public RegisterViewModel(IAuthService authService, ISessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;
    }

    private bool PoateInregistra() => !EsteInCurs;

    [RelayCommand(CanExecute = nameof(PoateInregistra))]
    private async Task RegisterAsync()
    {
        MesajEroare = ValideazaFormular();
        if (MesajEroare is not null)
        {
            return;
        }

        EsteInCurs = true;
        try
        {
            var adresaLivrare = string.IsNullOrWhiteSpace(AdresaLivrare) ? null : AdresaLivrare;
            var rezultat = await _authService.RegisterAsync(Nume, Prenume, Email, Telefon, adresaLivrare, Parola);

            if (!rezultat.Succes || rezultat.Utilizator is null)
            {
                MesajEroare = rezultat.MesajEroare ?? "Inregistrare esuata.";
                return;
            }

            _sessionService.SetCurrentUser(rezultat.Utilizator);
            InregistrareReusita?.Invoke(this, rezultat.Utilizator);
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private void NavigheazaLaLogin()
    {
        NavigheazaLaLoginRequested?.Invoke(this, EventArgs.Empty);
    }

    private string? ValideazaFormular()
    {
        if (string.IsNullOrWhiteSpace(Nume) || string.IsNullOrWhiteSpace(Prenume))
        {
            return "Numele si prenumele sunt obligatorii.";
        }

        if (string.IsNullOrWhiteSpace(Email) || !EmailRegex.IsMatch(Email))
        {
            return "Adresa de email nu este valida.";
        }

        if (string.IsNullOrWhiteSpace(Telefon) || !TelefonRegex.IsMatch(Telefon))
        {
            return "Numarul de telefon nu este valid.";
        }

        if (Parola.Length < LungimeMinimaParola)
        {
            return $"Parola trebuie sa aiba minim {LungimeMinimaParola} caractere.";
        }

        if (Parola != ConfirmareParola)
        {
            return "Parolele nu coincid.";
        }

        return null;
    }
}
