using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty]
    private ObservableCollection<CategorieGrupataViewModel> _categorii = new();

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    /// <summary>True doar daca e autentificat un Client - decide vizibilitatea butonului "Comanda" in View.</summary>
    public bool PoateComanda => _sessionService.EsteAutentificat && _sessionService.EsteClient;

    public MenuViewModel() : this(new MenuService(), SessionService.Instance)
    {
    }

    public MenuViewModel(IMenuService menuService, ISessionService sessionService)
    {
        _menuService = menuService;
        _sessionService = sessionService;
        _sessionService.CurrentUserChanged += (_, _) => OnPropertyChanged(nameof(PoateComanda));
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
