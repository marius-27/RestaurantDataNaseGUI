using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>Preparatele aproape de epuizare - doar pentru angajati autentificati (verificat de IStockService).</summary>
public partial class StocEpuizareViewModel : ViewModelBase
{
    private readonly IStockService _stockService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NuAreRezultate))]
    private ObservableCollection<PreparatEpuizareDto> _preparate = new();

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    public bool NuAreRezultate => Preparate.Count == 0;

    public bool PoateAdministra => _sessionService.EsteAngajat;

    public StocEpuizareViewModel() : this(new StockService(), SessionService.Instance)
    {
    }

    public StocEpuizareViewModel(IStockService stockService, ISessionService sessionService)
    {
        _stockService = stockService;
        _sessionService = sessionService;
        _sessionService.CurrentUserChanged += (_, _) => OnPropertyChanged(nameof(PoateAdministra));
    }

    [RelayCommand]
    private async Task IncarcaStocAsync()
    {
        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var preparate = await _stockService.GetPreparateApropiateDeEpuizareAsync();
            Preparate = new ObservableCollection<PreparatEpuizareDto>(preparate);
        }
        catch (UnauthorizedAccessException ex)
        {
            MesajEroare = ex.Message;
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-a putut incarca lista de stoc aproape de epuizare.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }
}
