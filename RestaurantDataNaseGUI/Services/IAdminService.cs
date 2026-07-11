using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// CRUD complet pentru Categorie/Alergen/Preparat/Meniu - doar pentru
// utilizatori cu TipUtilizator = "Angajat" (verificat prin ISessionService).
public interface IAdminService
{
    // Categorie
    Task<List<Categorie>> GetCategoriiAsync(CancellationToken cancellationToken = default);
    Task<AdminResult> CreeazaCategorieAsync(CategorieFormDto form, CancellationToken cancellationToken = default);
    Task<AdminResult> ActualizeazaCategorieAsync(CategorieFormDto form, CancellationToken cancellationToken = default);

    // Blocheaza stergerea daca exista Preparate/Meniuri asociate categoriei.
    Task<AdminResult> StergeCategorieAsync(int categorieId, CancellationToken cancellationToken = default);

    // Alergen
    Task<List<Alergen>> GetAlergeniAsync(CancellationToken cancellationToken = default);
    Task<AdminResult> CreeazaAlergenAsync(AlergenFormDto form, CancellationToken cancellationToken = default);
    Task<AdminResult> ActualizeazaAlergenAsync(AlergenFormDto form, CancellationToken cancellationToken = default);

    // Blocheaza stergerea daca alergenul e asociat unor preparate.
    Task<AdminResult> StergeAlergenAsync(int alergenId, CancellationToken cancellationToken = default);

    // Preparat
    // Include Categorie, PreparatAlergeni.Alergen si Imagini pentru fiecare preparat.
    Task<List<Preparat>> GetPreparateAsync(CancellationToken cancellationToken = default);

    Task<AdminResult> CreeazaPreparatAsync(PreparatFormDto form, CancellationToken cancellationToken = default);

    // Actualizeaza campurile scalare si inlocuieste integral listele de alergeni/imagini.
    Task<AdminResult> ActualizeazaPreparatAsync(PreparatFormDto form, CancellationToken cancellationToken = default);

    // Daca preparatul a fost deja folosit intr-o comanda, nu il sterge fizic -
    // il marcheaza indisponibil (soft-delete via SetPreparatIndisponibilAsync).
    // Daca face parte dintr-un meniu neutilizat inca, blocheaza stergerea. Altfel, sterge fizic.
    Task<AdminResult> StergePreparatAsync(int preparatId, CancellationToken cancellationToken = default);

    // Meniu
    // Include Categorie si MeniuPreparate.Preparat (componentele, cu cantitati) pentru fiecare meniu.
    Task<List<Meniu>> GetMeniuriAsync(CancellationToken cancellationToken = default);

    Task<AdminResult> CreeazaMeniuAsync(MeniuFormDto form, CancellationToken cancellationToken = default);

    // Actualizeaza campurile scalare si inlocuieste integral lista de preparate componente.
    Task<AdminResult> ActualizeazaMeniuAsync(MeniuFormDto form, CancellationToken cancellationToken = default);

    // Nu exista soft-delete pentru Meniu - daca a fost folosit intr-o comanda,
    // blocheaza stergerea. Altfel, sterge fizic.
    Task<AdminResult> StergeMeniuAsync(int meniuId, CancellationToken cancellationToken = default);
}
