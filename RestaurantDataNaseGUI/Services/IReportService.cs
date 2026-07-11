using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs.Reports;

namespace RestaurantDataNaseGUI.Services;

// Rapoarte pentru angajati - doar citire, doar agregari EF Core LINQ (fara
// SQL brut sau proceduri stocate). Fiecare metoda arunca
// UnauthorizedAccessException daca userul curent nu e angajat autentificat.
public interface IReportService
{
    // Numarul de comenzi din [dataStart, dataEnd], cate au fost anulate si
    // suma totala incasata (mancare + transport - discount, fara cele anulate), pe zile.
    Task<RaportVanzariDto> RaportVanzariPerioadaAsync(
        DateTime dataStart,
        DateTime dataEnd,
        CancellationToken cancellationToken = default);

    // Top preparate/meniuri dupa cantitatea totala comandata in
    // [dataStart, dataEnd] (comenzile anulate excluse).
    Task<List<PreparatVandutDto>> RaportPreparateCelMaiVanduteAsync(
        DateTime dataStart,
        DateTime dataEnd,
        int top = 10,
        CancellationToken cancellationToken = default);

    // Suma vanzarilor din [dataStart, dataEnd] grupata pe categorie
    // (comenzile anulate excluse).
    Task<List<VanzareCategorieDto>> RaportVanzariPeCategorieAsync(
        DateTime dataStart,
        DateTime dataEnd,
        CancellationToken cancellationToken = default);

    // Stocul curent al tuturor preparatelor, sortat pe categorie apoi denumire.
    Task<List<PreparatStocDto>> RaportStocCurentAsync(CancellationToken cancellationToken = default);
}
