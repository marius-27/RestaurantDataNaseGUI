using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>Alergen afisat ca CheckBox in PreparatAdminViewModel.</summary>
public partial class AlergenSelectabilViewModel : ViewModelBase
{
    public int AlergenId { get; }
    public string Denumire { get; }

    [ObservableProperty]
    private bool _esteSelectat;

    public AlergenSelectabilViewModel(int alergenId, string denumire, bool esteSelectat)
    {
        AlergenId = alergenId;
        Denumire = denumire;
        _esteSelectat = esteSelectat;
    }
}
