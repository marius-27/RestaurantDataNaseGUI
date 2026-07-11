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

// CRUD Meniu, doar pentru angajati (verificat de IAdminService). Componentele (preparat + cantitate) sunt o lista
// editabila: alegi preparat si cantitate, apesi "Adauga", poti sterge orice rand inainte de a salva.
public partial class MeniuAdminViewModel : ViewModelBase
{
    private readonly IAdminService _adminService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private ObservableCollection<Meniu> _meniuri = new();

    [ObservableProperty]
    private ObservableCollection<Categorie> _categoriiDisponibile = new();

    [ObservableProperty]
    private ObservableCollection<Preparat> _preparateDisponibile = new();

    [ObservableProperty]
    private ObservableCollection<MeniuComponentaViewModel> _componente = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsteEditare))]
    private int _idInEditare;

    [ObservableProperty]
    private string _denumire = string.Empty;

    [ObservableProperty]
    private Categorie? _categorieSelectata;

    [ObservableProperty]
    private Preparat? _preparatDeAdaugat;

    [ObservableProperty]
    private decimal _cantitateDeAdaugat = 1;

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    public bool EsteEditare => IdInEditare > 0;

    public bool PoateAdministra => _sessionService.EsteAngajat;

    public MeniuAdminViewModel() : this(new AdminService(), SessionService.Instance)
    {
    }

    public MeniuAdminViewModel(IAdminService adminService, ISessionService sessionService)
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
            var meniuri = await _adminService.GetMeniuriAsync();
            var categorii = await _adminService.GetCategoriiAsync();
            var preparate = await _adminService.GetPreparateAsync();

            Meniuri = new ObservableCollection<Meniu>(meniuri);
            CategoriiDisponibile = new ObservableCollection<Categorie>(categorii);
            PreparateDisponibile = new ObservableCollection<Preparat>(preparate);
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-au putut incarca meniurile.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private void SelecteazaPentruEditare(Meniu? meniu)
    {
        if (meniu is null)
        {
            return;
        }

        IdInEditare = meniu.Id;
        Denumire = meniu.Denumire;
        CategorieSelectata = CategoriiDisponibile.FirstOrDefault(c => c.Id == meniu.CategorieId);
        Componente = new ObservableCollection<MeniuComponentaViewModel>(
            meniu.MeniuPreparate.Select(mp => new MeniuComponentaViewModel(mp.PreparatId, mp.Preparat.Denumire, mp.CantitateInMeniu)));
        MesajEroare = null;
    }

    [RelayCommand]
    private void Anuleaza()
    {
        IdInEditare = 0;
        Denumire = string.Empty;
        CategorieSelectata = null;
        Componente = new ObservableCollection<MeniuComponentaViewModel>();
        PreparatDeAdaugat = null;
        CantitateDeAdaugat = 1;
        MesajEroare = null;
    }

    [RelayCommand]
    private void AdaugaComponenta()
    {
        MesajEroare = null;

        if (PreparatDeAdaugat is null)
        {
            MesajEroare = "Selecteaza un preparat de adaugat in meniu.";
            return;
        }

        if (CantitateDeAdaugat <= 0)
        {
            MesajEroare = "Cantitatea trebuie sa fie pozitiva.";
            return;
        }

        if (Componente.Any(c => c.PreparatId == PreparatDeAdaugat.Id))
        {
            MesajEroare = "Acest preparat este deja adaugat in meniu.";
            return;
        }

        Componente.Add(new MeniuComponentaViewModel(PreparatDeAdaugat.Id, PreparatDeAdaugat.Denumire, CantitateDeAdaugat));
        PreparatDeAdaugat = null;
        CantitateDeAdaugat = 1;
    }

    [RelayCommand]
    private void StergeComponenta(MeniuComponentaViewModel? componenta)
    {
        if (componenta is not null)
        {
            Componente.Remove(componenta);
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

        if (Componente.Count == 0)
        {
            MesajEroare = "Meniul trebuie sa contina cel putin un preparat.";
            return;
        }

        EsteInCurs = true;
        try
        {
            var form = new MeniuFormDto
            {
                Id = IdInEditare,
                Denumire = Denumire,
                CategorieId = CategorieSelectata.Id,
                Preparate = Componente
                    .Select(c => new MeniuPreparatFormDto { PreparatId = c.PreparatId, CantitateInMeniu = c.CantitateInMeniu })
                    .ToList(),
            };

            var rezultat = EsteEditare
                ? await _adminService.ActualizeazaMeniuAsync(form)
                : await _adminService.CreeazaMeniuAsync(form);

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
    private async Task StergeAsync(Meniu? meniu)
    {
        if (meniu is null)
        {
            return;
        }

        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var rezultat = await _adminService.StergeMeniuAsync(meniu.Id);
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
