using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

/// <summary>
/// Afiseaza meniul restaurantului. Nu depinde de autentificare pentru a se
/// putea incarca/afisa (un vizitator fara cont trebuie sa vada meniul) -
/// starea de sesiune se foloseste doar prin <see cref="PoateComanda"/>, ca
/// View-ul sa decida daca arata butonul de comanda.
/// </summary>
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

    /// <summary>True doar daca e autentificat un Client - decide vizibilitatea butonului "Comanda" in View.</summary>
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

    /// <summary>Adauga itemul in cos - nu face nimic daca userul curent nu poate comanda sau daca itemul e indisponibil.</summary>
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
