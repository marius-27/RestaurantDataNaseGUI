using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>
/// Un rand din ToateComenzileView: comanda (datele complete, inclusiv
/// clientul) + starea aleasa in dropdown-ul de tranzitie, dintre starile
/// urmatoare valide pentru starea ei curenta.
/// </summary>
public partial class ComandaAngajatRandViewModel : ViewModelBase
{
    public ComandaAngajatDto Comanda { get; }

    public ObservableCollection<string> StariDisponibile { get; }

    [ObservableProperty]
    private string? _stareSelectata;

    public ComandaAngajatRandViewModel(ComandaAngajatDto comanda, IEnumerable<string> stariDisponibile)
    {
        Comanda = comanda;
        StariDisponibile = new ObservableCollection<string>(stariDisponibile);
        _stareSelectata = StariDisponibile.FirstOrDefault();
    }
}
