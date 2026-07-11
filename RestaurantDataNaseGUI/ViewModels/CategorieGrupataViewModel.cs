using System.Collections.Generic;
using System.Collections.ObjectModel;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.ViewModels;

// Categorie afisata in MenuView, cu preparatele/meniurile ei ca lista bindabila.
public class CategorieGrupataViewModel
{
    public string Denumire { get; }
    public ObservableCollection<MeniuAfisareDto> Itemi { get; }

    public CategorieGrupataViewModel(string denumire, IEnumerable<MeniuAfisareDto> itemi)
    {
        Denumire = denumire;
        Itemi = new ObservableCollection<MeniuAfisareDto>(itemi);
    }
}
