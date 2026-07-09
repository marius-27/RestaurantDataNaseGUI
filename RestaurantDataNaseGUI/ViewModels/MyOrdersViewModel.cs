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

/// <summary>
/// Comenzile clientului autentificat curent: lista completa, urmarirea
/// comenzilor active (nelivrate, neanulate) si anularea unei comenzi active.
/// </summary>
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

    /// <summary>Comenzile nelivrate si neanulate - subset de ToateComenzile.</summary>
    public IEnumerable<ComandaClientDto> ComenziActive => ToateComenzile.Where(c => c.EsteActiva);

    /// <summary>Ce trebuie afisat in View, in functie de toggle-ul DoarActive.</summary>
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
