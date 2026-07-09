using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Utilizator
{
    public int Id { get; set; }
    public string Nume { get; set; } = string.Empty;
    public string Prenume { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;

    /// <summary>Optionala - fara sens pentru un Angajat.</summary>
    public string? AdresaLivrare { get; set; }

    /// <summary>Doar hash-ul parolei, niciodata parola in clar.</summary>
    public string ParolaHash { get; set; } = string.Empty;

    /// <summary>"Client" sau "Angajat" - vezi CK_Utilizator_TipUtilizator din schema.sql.</summary>
    public string TipUtilizator { get; set; } = string.Empty;

    public ICollection<Comanda> Comenzi { get; set; } = new List<Comanda>();
}
