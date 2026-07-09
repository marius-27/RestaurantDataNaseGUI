using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>
/// Un articol din cosul de comanda, inainte de a fi trimis la DB. Trebuie sa
/// aiba fie <see cref="PreparatId"/>, fie <see cref="MeniuId"/> completat -
/// niciodata ambele si niciodata niciunul (aceeasi regula ca ComandaDetaliu).
/// Observabil (CommunityToolkit) ca liniile din CartView (cantitate, subtotal)
/// sa se actualizeze automat cand ICartService modifica un articol existent.
/// </summary>
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

    /// <summary>PretUnitar * Cantitate - pentru afisarea liniei din cos.</summary>
    public decimal Subtotal => PretUnitar * Cantitate;
}
