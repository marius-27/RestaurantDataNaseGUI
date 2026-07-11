using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantDataNaseGUI.Models.DTOs;

// Articol din cos, inainte de trimitere la DB. Exact unul dintre
// PreparatId/MeniuId trebuie completat (ca la ComandaDetaliu). Observabil
// ca liniile din CartView sa se actualizeze automat.
public partial class ArticolCosDto : ObservableObject
{
    [ObservableProperty]
    private int? _preparatId;

    [ObservableProperty]
    private int? _meniuId;

    [ObservableProperty]
    private string _denumire = string.Empty;

    [ObservableProperty]
    private decimal _pretUnitar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    private decimal _cantitate;

    // PretUnitar * Cantitate, pentru linia din cos.
    public decimal Subtotal => PretUnitar * Cantitate;
}
