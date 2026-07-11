using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

// Comenzile clientului curent: lista completa, comenzile active (nelivrate,
// neanulate) si anularea uneia active.
public partial class MyOrdersViewModel : ViewModelBase
{
    private readonly IOrderService _orderService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComenziActive))]
    [NotifyPropertyChangedFor(nameof(ComenziDeAfisat))]
    private ObservableCollection<ComandaClientDto> _toateComenzile = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComenziDeAfisat))]
    private bool _doarActive;

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    // Comenzile nelivrate si neanulate, subset de ToateComenzile.
    public IEnumerable<ComandaClientDto> ComenziActive => ToateComenzile.Where(c => c.EsteActiva);

    // Ce se afiseaza in View, in functie de toggle-ul DoarActive.
    public IEnumerable<ComandaClientDto> ComenziDeAfisat => DoarActive ? ComenziActive : ToateComenzile;

    public MyOrdersViewModel() : this(new OrderService(), SessionService.Instance)
    {
    }

    public MyOrdersViewModel(IOrderService orderService, ISessionService sessionService)
    {
        _orderService = orderService;
        _sessionService = sessionService;
    }

    [RelayCommand]
    private async Task IncarcaComenziAsync()
    {
        MesajEroare = null;

        if (_sessionService.CurrentUser is null)
        {
            MesajEroare = "Trebuie sa fii autentificat pentru a vedea comenzile.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var comenzi = await _orderService.GetComenziClientAsync(_sessionService.CurrentUser.Id);
            ToateComenzile = new ObservableCollection<ComandaClientDto>(comenzi);
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-au putut incarca comenzile. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private async Task AnuleazaComandaAsync(int comandaId)
    {
        MesajEroare = null;

        if (_sessionService.CurrentUser is null)
        {
            MesajEroare = "Trebuie sa fii autentificat pentru a anula o comanda.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var rezultat = await _orderService.AnuleazaComandaAsync(comandaId, _sessionService.CurrentUser.Id);
            if (!rezultat.Succes)
            {
                MesajEroare = rezultat.MesajEroare ?? "Anularea comenzii a esuat.";
                return;
            }

            await IncarcaComenziAsync();
        }
        catch (Exception)
        {
            MesajEroare = "Anularea comenzii a esuat. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }
}
