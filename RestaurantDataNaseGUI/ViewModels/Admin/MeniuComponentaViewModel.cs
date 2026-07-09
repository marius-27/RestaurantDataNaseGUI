using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>O componenta (preparat + cantitate) din formularul de Meniu (MeniuAdminViewModel).</summary>
public partial class MeniuComponentaViewModel : ViewModelBase
{
    public int PreparatId { get; }
    public string Denumire { get; }

    [ObservableProperty]
    private decimal _cantitateInMeniu;

    public MeniuComponentaViewModel(int preparatId, string denumire, decimal cantitateInMeniu)
    {
        PreparatId = preparatId;
        Denumire = denumire;
        _cantitateInMeniu = cantitateInMeniu;
    }
}
