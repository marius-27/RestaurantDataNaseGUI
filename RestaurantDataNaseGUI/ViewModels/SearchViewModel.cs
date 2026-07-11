using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

// Tipurile de cautare in meniu suportate de SearchViewModel.
public enum TipCautare
{
    DupaDenumire,
    DupaAlergen,
}

// Cauta in meniu dupa denumire sau alergen (cu negare "contine"/"nu contine").
// Nu depinde de autentificare, la fel ca MenuViewModel - PoateComanda e
// folosit doar de template-ul de rezultate pentru butonul "Comanda".
public partial class SearchViewModel : ViewModelBase
{
    private readonly IMenuService _menuService;
    private readonly ISessionService _sessionService;
    private readonly ICartService _cartService;

    [ObservableProperty]
    private string _cuvantCheie = string.Empty;

    [ObservableProperty]
    private string? _alergenSelectat;

    [ObservableProperty]
    private ObservableCollection<string> _alergeniDisponibili = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NuContineAlergen))]
    private bool _contineAlergen = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsteCautareDupaDenumire))]
    [NotifyPropertyChangedFor(nameof(EsteCautareDupaAlergen))]
    private TipCautare _tipCautare = TipCautare.DupaDenumire;

    [ObservableProperty]
    private ObservableCollection<CategorieGrupataViewModel> _rezultateCautare = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CautaCommand))]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    [ObservableProperty]
    private string? _mesajNimicGasit;

    // Complementul lui ContineAlergen (toggle "nu contine").
    public bool NuContineAlergen
    {
        get => !ContineAlergen;
        set => ContineAlergen = !value;
    }

    public bool EsteCautareDupaDenumire
    {
        get => TipCautare == TipCautare.DupaDenumire;
        set
        {
            if (value)
            {
                TipCautare = TipCautare.DupaDenumire;
            }
        }
    }

    public bool EsteCautareDupaAlergen
    {
        get => TipCautare == TipCautare.DupaAlergen;
        set
        {
            if (value)
            {
                TipCautare = TipCautare.DupaAlergen;
            }
        }
    }

    // True doar daca e autentificat un Client - decide vizibilitatea butonului "Comanda".
    public bool PoateComanda => _sessionService.EsteAutentificat && _sessionService.EsteClient;

    public SearchViewModel() : this(new MenuService(), SessionService.Instance, CartService.Instance)
    {
    }

    public SearchViewModel(IMenuService menuService, ISessionService sessionService, ICartService cartService)
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
    private async Task IncarcaAlergeniAsync()
    {
        try
        {
            var alergeni = await _menuService.GetAlergeniDisponibiliAsync();
            AlergeniDisponibili = new ObservableCollection<string>(alergeni);
        }
        catch (Exception)
        {
            // Lista de alergeni e doar un ajutor pentru ComboBox - o esuare
            // aici nu trebuie sa blocheze cautarea dupa denumire.
        }
    }

    private bool PoateCauta() => !EsteInCurs;

    [RelayCommand(CanExecute = nameof(PoateCauta))]
    private async Task CautaAsync()
    {
        MesajEroare = null;
        MesajNimicGasit = null;

        if (TipCautare == TipCautare.DupaDenumire && string.IsNullOrWhiteSpace(CuvantCheie))
        {
            MesajEroare = "Introdu un cuvant cheie pentru cautare.";
            return;
        }

        if (TipCautare == TipCautare.DupaAlergen && string.IsNullOrWhiteSpace(AlergenSelectat))
        {
            MesajEroare = "Selecteaza un alergen.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var rezultate = TipCautare == TipCautare.DupaDenumire
                ? await _menuService.CautaDupaDenumireAsync(CuvantCheie)
                : await _menuService.CautaDupaAlergenAsync(AlergenSelectat!, ContineAlergen);

            RezultateCautare = new ObservableCollection<CategorieGrupataViewModel>(
                rezultate.Select(c => new CategorieGrupataViewModel(c.Denumire, c.Itemi)));

            if (RezultateCautare.Count == 0)
            {
                MesajNimicGasit = "Nu s-a gasit niciun preparat sau meniu pentru criteriile alese.";
            }
        }
        catch (Exception)
        {
            MesajEroare = "Cautarea a esuat. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }
}
