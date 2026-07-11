namespace RestaurantDataNaseGUI.ViewModels.Admin;

// Tipurile de raport din ReportsViewModel, cate unul per metoda din IReportService.
public enum TipRaport
{
    Vanzari,
    PreparateCelMaiVandute,
    VanzariPeCategorie,
    StocCurent,
}

// Optiune din selectorul de tip raport (ComboBox): enum + denumire afisata.
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
