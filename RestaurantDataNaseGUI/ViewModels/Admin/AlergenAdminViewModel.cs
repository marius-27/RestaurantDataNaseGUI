using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>CRUD pentru Alergen, accesibil doar angajatilor (verificat de IAdminService).</summary>
public partial class AlergenAdminViewModel : ViewModelBase
{
    private readonly IAdminService _adminService;
    private readonly ISessionService _sessionService;

    [ObservableProperty]
    private ObservableCollection<Alergen> _alergeni = new();

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

    public bool PoateAdministra => _sessionService.EsteAngajat;

    public AlergenAdminViewModel() : this(new AdminService(), SessionService.Instance)
    {
    }

    public AlergenAdminViewModel(IAdminService adminService, ISessionService sessionService)
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
            var alergeni = await _adminService.GetAlergeniAsync();
            Alergeni = new ObservableCollection<Alergen>(alergeni);
        }
        catch (Exception)
        {
            MesajEroare = "Nu s-au putut incarca alergenii.";
        }
        finally
        {
            EsteInCurs = false;
        }
    }

    [RelayCommand]
    private void SelecteazaPentruEditare(Alergen? alergen)
    {
        if (alergen is null)
        {
            return;
        }

        IdInEditare = alergen.Id;
        Denumire = alergen.Denumire;
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
            var form = new AlergenFormDto { Id = IdInEditare, Denumire = Denumire };
            var rezultat = EsteEditare
                ? await _adminService.ActualizeazaAlergenAsync(form)
                : await _adminService.CreeazaAlergenAsync(form);

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
    private async Task StergeAsync(Alergen? alergen)
    {
        if (alergen is null)
        {
            return;
        }

        MesajEroare = null;
        EsteInCurs = true;
        try
        {
            var rezultat = await _adminService.StergeAlergenAsync(alergen.Id);
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
