using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels;

/// <summary>Cele doua tipuri de cautare in meniu suportate de SearchViewModel.</summary>
public enum TipCautare
{
    DupaDenumire,
    DupaAlergen,
}

/// <summary>
/// Cauta in meniul restaurantului dupa denumire sau dupa alergen (cu negare -
/// "contine" / "nu contine"). Nu depinde de autentificare pentru a functiona,
/// la fel ca MenuViewModel - PoateComanda e folosit doar de template-ul de
/// rezultate ca sa decida vizibilitatea butonului "Comanda".
/// </summary>
public partial class SearchViewModel : ViewModelBase
{
    private readonly IMenuService _menuService;
    private readonly ISessionService _sessionService;

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

    /// <summary>Toggle bidirectional pentru "nu contine", complementul lui ContineAlergen.</summary>
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

    /// <summary>True doar daca e autentificat un Client - decide vizibilitatea butonului "Comanda" in template-ul de rezultate.</summary>
    public bool PoateComanda => _sessionService.EsteAutentificat && _sessionService.EsteClient;

    public SearchViewModel() : this(new MenuService(), SessionService.Instance)
    {
    }

    public SearchViewModel(IMenuService menuService, ISessionService sessionService)
    {
        _menuService = menuService;
        _sessionService = sessionService;
        _sessionService.CurrentUserChanged += (_, _) => OnPropertyChanged(nameof(PoateComanda));
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
