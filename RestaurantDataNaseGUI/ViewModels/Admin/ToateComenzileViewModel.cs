using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>
/// Vizualizarea tuturor comenzilor (sau doar a celor active) si schimbarea
/// starii unei comenzi - doar pentru angajati autentificati (verificat de
/// IOrderService).
/// </summary>
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

    /// <summary>Ce trebuie afisat in View, in functie de toggle-ul DoarActive.</summary>
    public IEnumerable<ComandaAngajatRandViewModel> ComenziDeAfisat =>
        DoarActive ? Comenzi.Where(c => c.Comanda.EsteActiva) : Comenzi;

    /// <summary>Pentru un shell viitor, care sa decida daca arata acest ecran doar angajatilor.</summary>
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
