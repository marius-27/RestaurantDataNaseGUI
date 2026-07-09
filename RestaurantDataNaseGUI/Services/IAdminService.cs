using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>
/// CRUD complet pentru Categorie/Alergen/Preparat/Meniu - accesibil doar
/// utilizatorilor cu TipUtilizator = "Angajat" (verificat de fiecare metoda
/// de Create/Update/Delete prin ISessionService).
/// </summary>
public interface IAdminService
{
    // Categorie
    Task<List<Categorie>> GetCategoriiAsync(CancellationToken cancellationToken = default);
    Task<AdminResult> CreeazaCategorieAsync(CategorieFormDto form, CancellationToken cancellationToken = default);
    Task<AdminResult> ActualizeazaCategorieAsync(CategorieFormDto form, CancellationToken cancellationToken = default);

    /// <summary>Blocheaza stergerea daca exista Preparate/Meniuri asociate acestei categorii.</summary>
    Task<AdminResult> StergeCategorieAsync(int categorieId, CancellationToken cancellationToken = default);

    // Alergen
    Task<List<Alergen>> GetAlergeniAsync(CancellationToken cancellationToken = default);
    Task<AdminResult> CreeazaAlergenAsync(AlergenFormDto form, CancellationToken cancellationToken = default);
    Task<AdminResult> ActualizeazaAlergenAsync(AlergenFormDto form, CancellationToken cancellationToken = default);

    /// <summary>Blocheaza stergerea daca alergenul e asociat unor preparate.</summary>
    Task<AdminResult> StergeAlergenAsync(int alergenId, CancellationToken cancellationToken = default);

    // Preparat
    /// <summary>Include Categorie, PreparatAlergeni.Alergen si Imagini pentru fiecare preparat.</summary>
    Task<List<Preparat>> GetPreparateAsync(CancellationToken cancellationToken = default);

    Task<AdminResult> CreeazaPreparatAsync(PreparatFormDto form, CancellationToken cancellationToken = default);

    /// <summary>Actualizeaza si campurile scalare, si listele de alergeni/imagini asociate (le inlocuieste integral).</summary>
    Task<AdminResult> ActualizeazaPreparatAsync(PreparatFormDto form, CancellationToken cancellationToken = default);

    /// <summary>
    /// Daca preparatul a fost deja folosit intr-o comanda (exista in
    /// ComandaDetaliu), NU il sterge fizic - il marcheaza indisponibil prin
    /// StoredProcedureRepository.SetPreparatIndisponibilAsync (soft-delete).
    /// Daca face parte dintr-un meniu (dar nu a fost inca folosit intr-o
    /// comanda), blocheaza stergerea cu un mesaj clar. Altfel, sterge fizic.
    /// </summary>
    Task<AdminResult> StergePreparatAsync(int preparatId, CancellationToken cancellationToken = default);

    // Meniu
    /// <summary>Include Categorie si MeniuPreparate.Preparat (componentele, cu cantitati) pentru fiecare meniu.</summary>
    Task<List<Meniu>> GetMeniuriAsync(CancellationToken cancellationToken = default);

    Task<AdminResult> CreeazaMeniuAsync(MeniuFormDto form, CancellationToken cancellationToken = default);

    /// <summary>Actualizeaza si campurile scalare, si lista de preparate componente (o inlocuieste integral).</summary>
    Task<AdminResult> ActualizeazaMeniuAsync(MeniuFormDto form, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nu exista soft-delete pentru Meniu in schema - daca a fost folosit
    /// intr-o comanda, blocheaza stergerea cu un mesaj clar. Altfel, sterge fizic.
    /// </summary>
    Task<AdminResult> StergeMeniuAsync(int meniuId, CancellationToken cancellationToken = default);
}
