using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

// CRUD pentru Preparat, doar pentru angajati (verificat de IAdminService).
// Alergenii sunt CheckBox-uri (AlergeniSelectabili), imaginile o lista
// editabila de cai (ImaginiPaths).
public partial class PreparatAdminViewModel : ViewModelBase
{
    private readonly IAdminService _adminService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private ObservableCollection<Preparat> _preparate = new();

    [ObservableProperty]
    private ObservableCollection<Categorie> _categoriiDisponibile = new();

    [ObservableProperty]
    private ObservableCollection<AlergenSelectabilViewModel> _alergeniSelectabili = new();

    [ObservableProperty]
    private ObservableCollection<string> _imaginiPaths = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsteEditare))]
    private int _idInEditare;

    [ObservableProperty]
    private string _denumire = string.Empty;

    [ObservableProperty]
    private decimal _pret;

    [ObservableProperty]
    private decimal _cantitatePortie;

    [ObservableProperty]
    private string _unitateMasura = string.Empty;

    [ObservableProperty]
    private decimal _cantitateTotalaRestaurant;

    [ObservableProperty]
    private Categorie? _categorieSelectata;

    [ObservableProperty]
    private bool _disponibil = true;

    [ObservableProperty]
    private string _caleImagineNoua = string.Empty;

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    public bool EsteEditare => IdInEditare > 0;

    public bool PoateAdministra => _sessionService.EsteAngajat;

    public PreparatAdminViewModel() : this(new AdminService(), SessionService.Instance)
    {
    }

    public PreparatAdminViewModel(IAdminService adminService, ISessionService sessionService)
    {
        _adminService = adminService;
        _sessionService = sessionService;
        _sessionService.CurrentUserChanged += (_, _) => OnPropertyChanged(nameof(PoateAdministra));
    }

    [RelayCommand]
    private async Task IncarcaAsync()
    {
        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var preparate = await _adminService.GetPreparateAsync();
            var categorii = await _adminService.GetCategoriiAsync();
            var alergeni = await _adminService.GetAlergeniAsync();

            Preparate = new ObservableCollection<Preparat>(preparate);
            CategoriiDisponibile = new ObservableCollection<Categorie>(categorii);
            AlergeniSelectabili = new ObservableCollection<AlergenSelectabilViewModel>(
                alergeni.Select(a => new AlergenSelectabilViewModel(a.Id, a.Denumire, esteSelectat: false)));
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-au putut incarca preparatele.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private void SelecteazaPentruEditare(Preparat? preparat)
    {
        if (preparat is null)
        {
            return;
        }

        IdInEditare = preparat.Id;
        Denumire = preparat.Denumire;
        Pret = preparat.Pret;
        CantitatePortie = preparat.CantitatePortie;
        UnitateMasura = preparat.UnitateMasura;
        CantitateTotalaRestaurant = preparat.CantitateTotalaRestaurant;
        Disponibil = preparat.Disponibil;
        CategorieSelectata = CategoriiDisponibile.FirstOrDefault(c => c.Id == preparat.CategorieId);
        ImaginiPaths = new ObservableCollection<string>(preparat.Imagini.Select(i => i.CalePoza));
        MesajEroare = null;

        var alergenIdsSelectati = preparat.PreparatAlergeni.Select(pa => pa.AlergenId).ToHashSet();
        foreach (var alergen in AlergeniSelectabili)
        {
            alergen.EsteSelectat = alergenIdsSelectati.Contains(alergen.AlergenId);
        }
    }

    [RelayCommand]
    private void Anuleaza()
    {
        IdInEditare = 0;
        Denumire = string.Empty;
        Pret = 0;
        CantitatePortie = 0;
        UnitateMasura = string.Empty;
        CantitateTotalaRestaurant = 0;
        Disponibil = true;
        CategorieSelectata = null;
        ImaginiPaths = new ObservableCollection<string>();
        CaleImagineNoua = string.Empty;
        MesajEroare = null;

        foreach (var alergen in AlergeniSelectabili)
        {
            alergen.EsteSelectat = false;
        }
    }

    [RelayCommand]
    private void AdaugaImagine()
    {
        var cale = CaleImagineNoua.Trim();
        if (string.IsNullOrWhiteSpace(cale))
        {
            return;
        }

        ImaginiPaths.Add(cale);
        CaleImagineNoua = string.Empty;
    }

    [RelayCommand]
    private void StergeImagine(string? cale)
    {
        if (cale is not null)
        {
            ImaginiPaths.Remove(cale);
        }
    }

    [RelayCommand]
    private async Task SalveazaAsync()
    {
        MesajEroare = null;

        if (CategorieSelectata is null)
        {
            MesajEroare = "Selecteaza o categorie.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var form = new PreparatFormDto
            {
                Id = IdInEditare,
                Denumire = Denumire,
                Pret = Pret,
                CantitatePortie = CantitatePortie,
                UnitateMasura = UnitateMasura,
                CantitateTotalaRestaurant = CantitateTotalaRestaurant,
                CategorieId = CategorieSelectata.Id,
                Disponibil = Disponibil,
                AlergenIds = AlergeniSelectabili.Where(a => a.EsteSelectat).Select(a => a.AlergenId).ToList(),
                ImaginiPaths = ImaginiPaths.ToList(),
            };

            var rezultat = EsteEditare
                ? await _adminService.ActualizeazaPreparatAsync(form)
                : await _adminService.CreeazaPreparatAsync(form);

            if (!rezultat.Succes)
            {
                MesajEroare = rezultat.MesajEroare ?? "Salvarea a esuat.";
                return;
            }

            Anuleaza();
            await IncarcaAsync();
        }
        catch (Exception)
        {
            MesajEroare = "Salvarea a esuat. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private async Task StergeAsync(Preparat? preparat)
    {
        if (preparat is null)
        {
            return;
        }

        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var rezultat = await _adminService.StergePreparatAsync(preparat.Id);
            if (!rezultat.Succes)
            {
                MesajEroare = rezultat.MesajEroare ?? "Stergerea a esuat.";
                return;
            }

            await IncarcaAsync();
        }
        catch (Exception)
        {
            MesajEroare = "Stergerea a esuat. Incearca din nou.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }
}
