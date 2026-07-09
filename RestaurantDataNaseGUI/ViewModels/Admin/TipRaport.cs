namespace RestaurantDataNaseGUI.ViewModels.Admin;

/// <summary>Tipurile de raport disponibile in ReportsViewModel, cate unul per metoda din IReportService.</summary>
public enum TipRaport
{
    Vanzari,
    PreparateCelMaiVandute,
    VanzariPeCategorie,
    StocCurent,
}

/// <summary>O optiune din selectorul de tip raport (ComboBox) - perechea enum + denumire afisata.</summary>
public class TipRaportOptiune
{
    public TipRaport Tip { get; }
    public string Denumire { get; }

    public TipRaportOptiune(TipRaport tip, string denumire)
    {
        Tip = tip;
        Denumire = denumire;
    }
}
