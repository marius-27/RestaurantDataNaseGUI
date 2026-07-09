using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantDataNaseGUI.Models.DTOs.Reports;
using RestaurantDataNaseGUI.Services;

namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>
/// Generarea rapoartelor - doar pentru angajati autentificati (verificat de
/// IReportService la fiecare apel). Rezultatul e tinut in patru colectii
/// separate, cate una per tip de raport (nu o singura colectie cu "coloane
/// dinamice") - fiecare tip de raport are propriile campuri, deci un
/// DataTemplate tipizat per tip e mai simplu de legat in Avalonia decat un
/// model generic de randuri/coloane.
/// </summary>
public partial class ReportsViewModel : ViewModelBase
{
    private readonly IReportService _reportService;
    private readonly ISessionService _sessionService;

    private bool _aGeneratRaport;

    public ObservableCollection<TipRaportOptiune> TipuriRaport { get; } = new()
    {
        new TipRaportOptiune(TipRaport.Vanzari, "Vanzari pe perioada"),
        new TipRaportOptiune(TipRaport.PreparateCelMaiVandute, "Preparate/meniuri cel mai vandute"),
        new TipRaportOptiune(TipRaport.VanzariPeCategorie, "Vanzari pe categorie"),
        new TipRaportOptiune(TipRaport.StocCurent, "Stoc curent"),
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AfiseazaVanzari))]
    [NotifyPropertyChangedFor(nameof(AfiseazaPreparate))]
    [NotifyPropertyChangedFor(nameof(AfiseazaCategorii))]
    [NotifyPropertyChangedFor(nameof(AfiseazaStoc))]
    [NotifyPropertyChangedFor(nameof(NecesitaInterval))]
    [NotifyPropertyChangedFor(nameof(NuAreRezultate))]
    private TipRaportOptiune _tipSelectat = null!;

    [ObservableProperty]
    private DateTimeOffset? _dataStart = DateTimeOffset.Now.Date.AddDays(-30);

    [ObservableProperty]
    private DateTimeOffset? _dataEnd = DateTimeOffset.Now.Date;

    /// <summary>Doar pentru raportul "Preparate cel mai vandute" - cate randuri se afiseaza.</summary>
    [ObservableProperty]
    private decimal _top = 10;

    [ObservableProperty]
    private bool _esteInCurs;

    [ObservableProperty]
    private string? _mesajEroare;

    [ObservableProperty]
    private string? _mesajExport;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NuAreRezultate))]
    private RaportVanzariDto? _raportVanzari;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NuAreRezultate))]
    private ObservableCollection<PreparatVandutDto> _preparateVandute = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NuAreRezultate))]
    private ObservableCollection<VanzareCategorieDto> _vanzariPeCategorie = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NuAreRezultate))]
    private ObservableCollection<PreparatStocDto> _stocCurent = new();

    public bool AfiseazaVanzari => TipSelectat.Tip == TipRaport.Vanzari;
    public bool AfiseazaPreparate => TipSelectat.Tip == TipRaport.PreparateCelMaiVandute;
    public bool AfiseazaCategorii => TipSelectat.Tip == TipRaport.VanzariPeCategorie;
    public bool AfiseazaStoc => TipSelectat.Tip == TipRaport.StocCurent;

    /// <summary>Raportul de stoc curent nu are interval de date (e mereu "acum").</summary>
    public bool NecesitaInterval => TipSelectat.Tip != TipRaport.StocCurent;

    public bool NuAreRezultate => _aGeneratRaport && TipSelectat.Tip switch
    {
        TipRaport.Vanzari => RaportVanzari is { Zile.Count: 0 },
        TipRaport.PreparateCelMaiVandute => PreparateVandute.Count == 0,
        TipRaport.VanzariPeCategorie => VanzariPeCategorie.Count == 0,
        TipRaport.StocCurent => StocCurent.Count == 0,
        _ => false,
    };

    public bool PoateAdministra => _sessionService.EsteAngajat;

    public ReportsViewModel() : this(new ReportService(), SessionService.Instance)
    {
    }

    public ReportsViewModel(IReportService reportService, ISessionService sessionService)
    {
        _reportService = reportService;
        _sessionService = sessionService;
        _sessionService.CurrentUserChanged += (_, _) => OnPropertyChanged(nameof(PoateAdministra));
        TipSelectat = TipuriRaport[0];
    }

    [RelayCommand]
    private async Task GenereazaRaportAsync()
    {
        MesajEroare = null;
        MesajExport = null;
        EsteInCurs = true;
        _aGeneratRaport = false;

        try
        {
            var start = (DataStart ?? DateTimeOffset.Now.AddDays(-30)).Date;
            var end = (DataEnd ?? DateTimeOffset.Now).Date;

            if (NecesitaInterval && start > end)
            {
                MesajEroare = "Data de inceput trebuie sa fie inainte de data de sfarsit.";
                return;
            }

            switch (TipSelectat.Tip)
            {
                case TipRaport.Vanzari:
                    RaportVanzari = await _reportService.RaportVanzariPerioadaAsync(start, end);
                    break;

                case TipRaport.PreparateCelMaiVandute:
                    var preparate = await _reportService.RaportPreparateCelMaiVanduteAsync(start, end, (int)Top);
                    PreparateVandute = new ObservableCollection<PreparatVandutDto>(preparate);
                    break;

                case TipRaport.VanzariPeCategorie:
                    var categorii = await _reportService.RaportVanzariPeCategorieAsync(start, end);
                    VanzariPeCategorie = new ObservableCollection<VanzareCategorieDto>(categorii);
                    break;

                case TipRaport.StocCurent:
                    var stoc = await _reportService.RaportStocCurentAsync();
                    StocCurent = new ObservableCollection<PreparatStocDto>(stoc);
                    break;
            }

            _aGeneratRaport = true;
        }
        catch (UnauthorizedAccessException ex)
        {
            MesajEroare = ex.Message;
        }
        catch (Exception)
        {
            MesajEroare = "Generarea raportului a esuat.";
        }
        finally
        {
            EsteInCurs = false;
            OnPropertyChanged(nameof(NuAreRezultate));
        }
    }

    [RelayCommand]
    private void ExportaCsv()
    {
        MesajEroare = null;
        MesajExport = null;

        if (!_aGeneratRaport)
        {
            MesajEroare = "Genereaza mai intai un raport.";
            return;
        }

        try
        {
            var (numeRaport, linii) = ConstruiesteRandurileCsv();
            if (linii is null)
            {
                MesajEroare = "Niciun raport generat pentru export.";
                return;
            }

            var folder = Path.Combine(AppContext.BaseDirectory, "Rapoarte");
            Directory.CreateDirectory(folder);
            var cale = Path.Combine(folder, $"raport_{numeRaport}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            File.WriteAllLines(cale, linii, Encoding.UTF8);

            MesajExport = $"Raport exportat: {cale}";
        }
        catch (Exception)
        {
            MesajEroare = "Exportul CSV a esuat.";
        }
    }

    private (string NumeRaport, System.Collections.Generic.List<string>? Linii) ConstruiesteRandurileCsv()
    {
        var linii = new System.Collections.Generic.List<string>();

        switch (TipSelectat.Tip)
        {
            case TipRaport.Vanzari when RaportVanzari is not null:
                linii.Add("Data,NumarComenzi,NumarComenziAnulate,SumaIncasata");
                foreach (var zi in RaportVanzari.Zile)
                {
                    linii.Add(string.Join(",",
                        EscapeCsv(zi.Data.ToString("yyyy-MM-dd")),
                        EscapeCsv(zi.NumarComenzi.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsv(zi.NumarComenziAnulate.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsv(zi.SumaIncasata.ToString(CultureInfo.InvariantCulture))));
                }

                linii.Add(string.Empty);
                linii.Add(string.Join(",",
                    EscapeCsv("Total"),
                    EscapeCsv(RaportVanzari.NumarComenzi.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(RaportVanzari.NumarComenziAnulate.ToString(CultureInfo.InvariantCulture)),
                    EscapeCsv(RaportVanzari.SumaTotalaIncasata.ToString(CultureInfo.InvariantCulture))));
                return ("vanzari", linii);

            case TipRaport.PreparateCelMaiVandute:
                linii.Add("Denumire,Tip,Categorie,CantitateTotala,SumaIncasata");
                foreach (var p in PreparateVandute)
                {
                    linii.Add(string.Join(",",
                        EscapeCsv(p.Denumire),
                        EscapeCsv(p.Tip),
                        EscapeCsv(p.Categorie),
                        EscapeCsv(p.CantitateTotalaComandata.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsv(p.SumaIncasata.ToString(CultureInfo.InvariantCulture))));
                }

                return ("preparate-vandute", linii);

            case TipRaport.VanzariPeCategorie:
                linii.Add("Categorie,CantitateTotala,SumaIncasata");
                foreach (var c in VanzariPeCategorie)
                {
                    linii.Add(string.Join(",",
                        EscapeCsv(c.Categorie),
                        EscapeCsv(c.CantitateTotala.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsv(c.SumaIncasata.ToString(CultureInfo.InvariantCulture))));
                }

                return ("vanzari-categorie", linii);

            case TipRaport.StocCurent:
                linii.Add("Denumire,Categorie,Cantitate,UnitateMasura,Disponibil");
                foreach (var s in StocCurent)
                {
                    linii.Add(string.Join(",",
                        EscapeCsv(s.Denumire),
                        EscapeCsv(s.Categorie),
                        EscapeCsv(s.CantitateTotalaRestaurant.ToString(CultureInfo.InvariantCulture)),
                        EscapeCsv(s.UnitateMasura),
                        EscapeCsv(s.Disponibil ? "Da" : "Nu")));
                }

                return ("stoc-curent", linii);

            default:
                return (string.Empty, null);
        }
    }

    /// <summary>Escaping minim CSV: incadreaza in ghilimele daca valoarea contine virgula, ghilimele sau linie noua.</summary>
    private static string EscapeCsv(string? valoare)
    {
        valoare ??= string.Empty;
        if (valoare.Contains(',') || valoare.Contains('"') || valoare.Contains('\n') || valoare.Contains('\r'))
        {
            return "\"" + valoare.Replace("\"", "\"\"") + "\"";
        }

        return valoare;
    }
}
