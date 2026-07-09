using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Meniu
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public int CategorieId { get; set; }

    // NOTA: intentionat nu exista o proprietate "Pret" aici. Pretul se
    // calculeaza dinamic in baza de date, vezi dbo.fn_CalculeazaPretMeniu.

    public Categorie Categorie { get; set; } = null!;
    public ICollection<MeniuPreparat> MeniuPreparate { get; set; } = new List<MeniuPreparat>();
    public ICollection<ComandaDetaliu> ComandaDetalii { get; set; } = new List<ComandaDetaliu>();
}
