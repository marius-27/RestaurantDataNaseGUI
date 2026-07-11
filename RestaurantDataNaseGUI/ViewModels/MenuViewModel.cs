using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

// Afiseaza meniul restaurantului; nu depinde de autentificare, ca un
// vizitator fara cont sa il poata vedea. Sesiunea se foloseste doar prin
// PoateComanda, ca View-ul sa decida daca arata butonul de comanda.
public partial class MenuViewModel : ViewModelBase
{
    private readonly IMenuService _menuService;
    private readonly ISessionService _sessionService;
    private readonly ICartService _cartService;

    [ObservableProperty]
    private ObservableCollection<CategorieGrupataViewModel> _categorii = new();

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    // True doar daca e autentificat un Client - decide vizibilitatea butonului "Comanda".
    public bool PoateComanda => _sessionService.EsteAutentificat && _sessionService.EsteClient;

    public MenuViewModel() : this(new MenuService(), SessionService.Instance, CartService.Instance)
    {
    }

    public MenuViewModel(IMenuService menuService, ISessionService sessionService, ICartService cartService)
    {
        _menuService = menuService;
        _sessionService = sessionService;
        _cartService = cartService;
        _sessionService.CurrentUserChanged += (_, _) => OnPropertyChanged(nameof(PoateComanda));
    }

    // Adauga itemul in cos, doar daca userul poate comanda si itemul e disponibil.
    [RelayCommand]
    private void AdaugaInCos(MeniuAfisareDto? item)
    {
        if (item is null || !PoateComanda || item.EsteIndisponibil)
        {
            return;
        }

        _cartService.AdaugaInCos(item);
    }

    [RelayCommand]
    private async Task IncarcaMeniuAsync()
    {
        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var categoriiMeniu = await _menuService.GetMeniuRestaurantAsync();
            Categorii = new ObservableCollection<CategorieGrupataViewModel>(
                categoriiMeniu.Select(c => new CategorieGrupataViewModel(c.Denumire, c.Itemi)));
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-a putut incarca meniul restaurantului. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }
}
