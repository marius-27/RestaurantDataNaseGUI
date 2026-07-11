using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

// Vizualizeaza toate comenzile (sau doar cele active) si schimba starea lor,
// doar pentru angajati (verificat de IOrderService).
public partial class ToateComenzileViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComenziDeAfisat))]
    private ObservableCollection<ComandaAngajatRandViewModel> _comenzi = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComenziDeAfisat))]
    private bool _doarActive;

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    // Ce se afiseaza in View, in functie de toggle-ul DoarActive.
    public IEnumerable<ComandaAngajatRandViewModel> ComenziDeAfisat =>
        DoarActive ? Comenzi.Where(c => c.Comanda.EsteActiva) : Comenzi;

    // Pentru un shell viitor: arata acest ecran doar angajatilor.
    public bool PoateAdministra => _sessionService.EsteAngajat;

    public ToateComenzileViewModel() : this(new OrderService(), SessionService.Instance)
    {
    }

    public ToateComenzileViewModel(IOrderService orderService, ISessionService sessionService)
    {
        _orderService = orderService;
        _sessionService = sessionService;
        _sessionService.CurrentUserChanged += (_, _) => OnPropertyChanged(nameof(PoateAdministra));
    }

    [RelayCommand]
    private async Task IncarcaComenziAsync()
    {
        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var comenzi = await _orderService.GetToateComenzileAsync();
            Comenzi = new ObservableCollection<ComandaAngajatRandViewModel>(
                comenzi.Select(c => new ComandaAngajatRandViewModel(c, _orderService.GetStariUrmatoarePosibile(c.Stare))));
        }
        catch (UnauthorizedAccessException ex)
        {
            MesajEroare = ex.Message;
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-au putut incarca comenzile.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private async Task SchimbaStareAsync(ComandaAngajatRandViewModel? rand)
    {
        MesajEroare = null;

        if (rand is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(rand.StareSelectata))
        {
            MesajEroare = "Selecteaza o stare noua pentru aceasta comanda.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var rezultat = await _orderService.SchimbaStareComandaAsync(rand.Comanda.ComandaId, rand.StareSelectata);
            if (!rezultat.Succes)
            {
                MesajEroare = rezultat.MesajEroare ?? "Schimbarea starii a esuat.";
                return;
            }

            await IncarcaComenziAsync();
        }
        catch (Exception)
        {
            MesajEroare = "Schimbarea starii a esuat. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }
}
