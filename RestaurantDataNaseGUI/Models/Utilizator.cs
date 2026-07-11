using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models;

public class Utilizator
{
    public int Id { get; set; }
    public string Nume { get; set; } = string.Empty;
    public string Prenume { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Telefon { get; set; } = string.Empty;

    // Optionala - fara sens pentru un Angajat.
    public string? AdresaLivrare { get; set; }

    // Doar hash-ul parolei, niciodata parola in clar.
    public string ParolaHash { get; set; } = string.Empty;

    // "Client" sau "Angajat" - vezi CK_Utilizator_TipUtilizator din schema.sql.
    public string TipUtilizator { get; set; } = string.Empty;

    public ICollection<Comanda> Comenzi { get; set; } = new List<Comanda>();
}
