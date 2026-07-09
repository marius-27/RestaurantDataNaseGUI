using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

/// <summary>
/// Model unificat pentru afisarea meniului restaurantului: reprezinta fie un
/// Preparat individual, fie un Meniu compus - vezi <see cref="EsteMeniuCompus"/>.
/// </summary>
public class MeniuAfisareDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal Pret { get; set; }

    /// <summary>Cantitatea unei portii; null pentru meniuri compuse (nu au o singura portie).</summary>
    public decimal? CantitatePortie { get; set; }

    /// <summary>Unitatea de masura a portiei; null pentru meniuri compuse.</summary>
    public string? UnitateMasura { get; set; }

    public IReadOnlyList<string> ListaAlergeni { get; set; } = Array.Empty<string>();

    /// <summary>Caile catre imaginile preparatului; goala pentru meniuri compuse (Meniu nu are imagine proprie in schema).</summary>
    public IReadOnlyList<string> ListaImaginiPath { get; set; } = Array.Empty<string>();

    /// <summary>True pentru un Meniu (compus din mai multe preparate), false pentru un Preparat individual.</summary>
    public bool EsteMeniuCompus { get; set; }

    /// <summary>
    /// True daca preparatul e marcat indisponibil, sau (pentru meniuri) daca
    /// cel putin un preparat component e indisponibil.
    /// </summary>
    public bool EsteIndisponibil { get; set; }

    public string? PrimaImaginePath => ListaImaginiPath.Count > 0 ? ListaImaginiPath[0] : null;

    public string CantitateText => CantitatePortie.HasValue
        ? $"{CantitatePortie.Value:0.##} {UnitateMasura}"
        : string.Empty;

    public string AlergeniText => ListaAlergeni.Count > 0
        ? string.Join(", ", ListaAlergeni)
        : "Fara alergeni declarati";
}
