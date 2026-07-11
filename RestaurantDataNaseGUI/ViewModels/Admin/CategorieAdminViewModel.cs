using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

// CRUD Categorie, doar pentru angajati (verificat de IAdminService).
public partial class CategorieAdminViewModel : ViewModelBase
{
    private readonly IAdminService _adminService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private ObservableCollection<Categorie> _categorii = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsteEditare))]
    private int _idInEditare;

    [ObservableProperty]
    private string _denumire = string.Empty;

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    public bool EsteEditare => IdInEditare > 0;

    // Pentru un shell viitor, care decide daca arata ecranul doar angajatilor.
    public bool PoateAdministra => _sessionService.EsteAngajat;

    public CategorieAdminViewModel() : this(new AdminService(), SessionService.Instance)
    {
    }

    public CategorieAdminViewModel(IAdminService adminService, ISessionService sessionService)
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
            var categorii = await _adminService.GetCategoriiAsync();
            Categorii = new ObservableCollection<Categorie>(categorii);
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-au putut incarca categoriile.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private void SelecteazaPentruEditare(Categorie? categorie)
    {
        if (categorie is null)
        {
            return;
        }

        IdInEditare = categorie.Id;
        Denumire = categorie.Denumire;
        MesajEroare = null;
    }

    [RelayCommand]
    private void Anuleaza()
    {
        IdInEditare = 0;
        Denumire = string.Empty;
        MesajEroare = null;
    }

    [RelayCommand]
    private async Task SalveazaAsync()
    {
        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var form = new CategorieFormDto { Id = IdInEditare, Denumire = Denumire };
            var rezultat = EsteEditare
                ? await _adminService.ActualizeazaCategorieAsync(form)
                : await _adminService.CreeazaCategorieAsync(form);

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
    private async Task StergeAsync(Categorie? categorie)
    {
        if (categorie is null)
        {
            return;
        }

        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var rezultat = await _adminService.StergeCategorieAsync(categorie.Id);
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
