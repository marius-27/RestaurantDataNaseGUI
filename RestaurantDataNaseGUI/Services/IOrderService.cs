using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

/// <summary>Calculul costului si crearea comenzilor pentru clienti autentificati.</summary>
public interface IOrderService
{
    /// <summary>
    /// Calculeaza subtotalul, discountul (daca se aplica) si costul de
    /// transport pentru cosul dat, fara sa creeze nicio comanda - util
    /// pentru un ecran de "cos" care afiseaza totalul inainte de confirmare.
    /// </summary>
    Task<CalculComandaDto> CalculeazaCostComandaAsync(
        List<ArticolCosDto> articole,
        int utilizatorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creeaza o comanda noua pentru <paramref name="utilizatorId"/> (trebuie
    /// sa fie clientul autentificat curent - vezi ISessionService), dupa ce
    /// verifica disponibilitatea tuturor articolelor din cos. Calculeaza
    /// costurile prin <see cref="CalculeazaCostComandaAsync"/> si insereaza
    /// antetul + liniile de comanda intr-o singura tranzactie EF Core.
    /// </summary>
    Task<OrderResult> CreeazaComandaAsync(
        List<ArticolCosDto> articole,
        int utilizatorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toate comenzile lui <paramref name="utilizatorId"/>, cu articolele
    /// lor, cele mai recente primele. Foloseste
    /// StoredProcedureRepository.GetComenziClientCuDetaliiAsync (un rand per
    /// articol de comanda) si grupeaza randurile dupa ComandaId.
    /// </summary>
    Task<List<ComandaClientDto>> GetComenziClientAsync(
        int utilizatorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Anuleaza o comanda activa (nici "livrata", nici deja "anulata") a lui
    /// <paramref name="utilizatorId"/>. Verifica prin EF Core ca acea comanda
    /// chiar apartine utilizatorului dat inainte de a-i schimba starea.
    /// </summary>
    Task<OrderResult> AnuleazaComandaAsync(
        int comandaId,
        int utilizatorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toate comenzile, ale tuturor clientilor, cu datele clientului incluse
    /// - doar pentru angajati autentificati (arunca UnauthorizedAccessException
    /// altfel). Sortate descrescator dupa data.
    /// </summary>
    Task<List<ComandaAngajatDto>> GetToateComenzileAsync(CancellationToken cancellationToken = default);

    /// <summary>Ca GetToateComenzileAsync, filtrat pe comenzile active (nelivrate, neanulate).</summary>
    Task<List<ComandaAngajatDto>> GetComenziActiveAngajatAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starile in care se poate trece direct din <paramref name="stareCurenta"/>
    /// (vezi tranzitiile valide documentate in OrderService). Lista goala
    /// daca <paramref name="stareCurenta"/> e o stare finala sau necunoscuta.
    /// </summary>
    IReadOnlyList<string> GetStariUrmatoarePosibile(string stareCurenta);

    /// <summary>
    /// Schimba starea unei comenzi - doar pentru angajati autentificati.
    /// Respinge tranzitia daca starea curenta e finala ("livrata"/"anulata")
    /// sau daca <paramref name="stareNoua"/> nu e o tranzitie valida din
    /// starea curenta (vezi GetStariUrmatoarePosibile). Daca
    /// <paramref name="stareNoua"/> e "se pregateste", dupa actualizarea
    /// starii apeleaza automat
    /// StoredProcedureRepository.UpdateCantitateTotalaLaComandaAsync, in
    /// aceeasi tranzactie - daca scaderea stocului esueaza (stoc
    /// insuficient), intreaga schimbare de stare e anulata.
    /// </summary>
    Task<OrderResult> SchimbaStareComandaAsync(
        int comandaId,
        string stareNoua,
        CancellationToken cancellationToken = default);
}
