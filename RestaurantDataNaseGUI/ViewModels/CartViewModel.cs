using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

/// <summary>Parametrul comenzii ModificaCantitateCommand: articolul din cos + noua cantitate ceruta.</summary>
public sealed record ModificaCantitateParametru(ArticolCosDto Articol, decimal CantitateNoua);

/// <summary>
/// Cosul de comanda al clientului autentificat curent. Articolele vin direct
/// din ICartService (o singura colectie in-memory, partajata cu
/// MenuViewModel/SearchViewModel prin AdaugaInCosCommand) - CartViewModel doar
/// le afiseaza si recalculeaza costul total prin IOrderService de fiecare
/// data cand se schimba cosul (ICartService.CosSchimbat).
/// </summary>
public partial class CartViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _sessionService;
    private readonly ICartService _cartService;

    [ObservableProperty]
    private CalculComandaDto? _calculCurent;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TrimiteComandaCommand))]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    [ObservableProperty]
    private string? _mesajSucces;

    /// <summary>Aceeasi colectie ca ICartService.Articole - modificarile din cos se reflecta direct aici.</summary>
    public ObservableCollection<ArticolCosDto> Articole => _cartService.Articole;

    public bool CosGol => Articole.Count == 0;

    /// <summary>Se declanseaza dupa ce comanda a fost trimisa cu succes, cu codul unic al comenzii.</summary>
    public event EventHandler<string>? ComandaCreataSucces;

    public CartViewModel() : this(new OrderService(), SessionService.Instance, CartService.Instance)
    {
    }

    public CartViewModel(IOrderService orderService, ISessionService sessionService, ICartService cartService)
    {
        _orderService = orderService;
        _sessionService = sessionService;
        _cartService = cartService;

        _cartService.CosSchimbat += async (_, _) => await SincronizeazaDupaSchimbareAsync();
        _sessionService.CurrentUserChanged += (_, _) => TrimiteComandaCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task IncarcaCosAsync()
    {
        await RecalculeazaCostAsync();
    }

    [RelayCommand]
    private void StergeArticol(ArticolCosDto? articol)
    {
        if (articol is not null)
        {
            _cartService.StergeArticol(articol);
        }
    }

    [RelayCommand]
    private void ModificaCantitate(ModificaCantitateParametru? parametru)
    {
        if (parametru is not null)
        {
            _cartService.ModificaCantitate(parametru.Articol, parametru.CantitateNoua);
        }
    }

    private bool PoateTrimiteComanda() =>
        !EsteInCurs && Articole.Count > 0 && _sessionService.EsteAutentificat && _sessionService.EsteClient;

    [RelayCommand(CanExecute = nameof(PoateTrimiteComanda))]
    private async Task TrimiteComandaAsync()
    {
        MesajEroare = null;
        MesajSucces = null;

        if (!_sessionService.EsteAutentificat || !_sessionService.EsteClient || _sessionService.CurrentUser is null)
        {
            MesajEroare = "Trebuie sa fii autentificat ca si client pentru a trimite o comanda.";
            return;
        }

        if (Articole.Count == 0)
        {
            MesajEroare = "Cosul este gol.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var articole = Articole.ToList();
            var rezultat = await _orderService.CreeazaComandaAsync(articole, _sessionService.CurrentUser.Id);

            if (!rezultat.Succes || rezultat.CodUnic is null)
            {
                MesajEroare = rezultat.MesajEroare ?? "Trimiterea comenzii a esuat.";
                return;
            }

            _cartService.GolesteCos();
            CalculCurent = null;
            MesajSucces = $"Comanda a fost trimisa cu succes! Cod: {rezultat.CodUnic}";
            ComandaCreataSucces?.Invoke(this, rezultat.CodUnic);
        }
        catch (Exception)
        {
            MesajEroare = "Trimiterea comenzii a esuat. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    private async Task SincronizeazaDupaSchimbareAsync()
    {
        OnPropertyChanged(nameof(CosGol));
        TrimiteComandaCommand.NotifyCanExecuteChanged();
        await RecalculeazaCostAsync();
    }

    private async Task RecalculeazaCostAsync()
    {
        if (Articole.Count == 0 || _sessionService.CurrentUser is null)
        {
            CalculCurent = null;
            return;
        }

        try
        {
            var articole = Articole.ToList();
            CalculCurent = await _orderService.CalculeazaCostComandaAsync(articole, _sessionService.CurrentUser.Id);
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-a putut calcula costul comenzii.";
        }
    }
}
