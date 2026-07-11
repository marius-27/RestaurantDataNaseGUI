using System;
using System.Collections.Generic;

namespace RestaurantDataNaseGUI.Models.DTOs;

// Model unificat pentru afisare: Preparat individual sau Meniu compus -
// vezi EsteMeniuCompus.
public class MeniuAfisareDto
{
    public int Id { get; set; }
    public string Denumire { get; set; } = string.Empty;
    public string Categorie { get; set; } = string.Empty;
    public decimal Pret { get; set; }

    // Cantitatea unei portii; null pentru meniuri compuse.
    public decimal? CantitatePortie { get; set; }

    // Unitatea de masura a portiei; null pentru meniuri compuse.
    public string? UnitateMasura { get; set; }

    public IReadOnlyList<string> ListaAlergeni { get; set; } = Array.Empty<string>();

    // Caile imaginilor preparatului; goala pentru meniuri compuse (Meniu nu are imagine proprie).
    public IReadOnlyList<string> ListaImaginiPath { get; set; } = Array.Empty<string>();

    // True pentru Meniu compus, false pentru Preparat individual.
    public bool EsteMeniuCompus { get; set; }

    // True daca preparatul e indisponibil, sau (pentru meniuri) un component
    // e indisponibil.
    public bool EsteIndisponibil { get; set; }

    public string? PrimaImaginePath => ListaImaginiPath.Count > 0 ? ListaImaginiPath[0] : null;

    public string CantitateText => CantitatePortie.HasValue
        ? $"{CantitatePortie.Value:0.##} {UnitateMasura}"
        : string.Empty;

    public string AlergeniText => ListaAlergeni.Count > 0
        ? string.Join(", ", ListaAlergeni)
        : "Fara alergeni declarati";
}
