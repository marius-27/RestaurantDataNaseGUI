using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs.Reports;

namespace RestaurantDataNaseGUI.Services;

/// <summary>
/// Rapoarte pentru angajati - doar citire, doar agregari EF Core LINQ
/// (Sum/Count/GroupBy parametrizate automat de provider, fara SQL brut sau
/// proceduri stocate). Fiecare metoda arunca UnauthorizedAccessException
/// daca userul curent nu e un angajat autentificat, la fel ca
/// IOrderService.GetToateComenzileAsync/IStockService.GetPreparateApropiateDeEpuizareAsync.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Numarul de comenzi plasate in [dataStart, dataEnd] (toate zilele
    /// incluse), cate dintre acestea au fost anulate si suma totala incasata
    /// (mancare + transport - discount, comenzile anulate excluse), plus
    /// defalcarea pe zile.
    /// </summary>
    Task<RaportVanzariDto> RaportVanzariPerioadaAsync(
        DateTime dataStart,
        DateTime dataEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Top preparate/meniuri dupa cantitatea totala comandata in
    /// [dataStart, dataEnd] (comenzile anulate sunt excluse).
    /// </summary>
    Task<List<PreparatVandutDto>> RaportPreparateCelMaiVanduteAsync(
        DateTime dataStart,
        DateTime dataEnd,
        int top = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suma vanzarilor din [dataStart, dataEnd] grupata pe categoria
    /// preparatelor/meniurilor comandate (comenzile anulate sunt excluse).
    /// </summary>
    Task<List<VanzareCategorieDto>> RaportVanzariPeCategorieAsync(
        DateTime dataStart,
        DateTime dataEnd,
        CancellationToken cancellationToken = default);

    /// <summary>Stocul curent al tuturor preparatelor, sortat pe categorie apoi denumire.</summary>
    Task<List<PreparatStocDto>> RaportStocCurentAsync(CancellationToken cancellationToken = default);
}
