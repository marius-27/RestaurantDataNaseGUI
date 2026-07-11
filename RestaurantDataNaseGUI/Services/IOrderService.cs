using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RestaurantDataNaseGUI.Models.DTOs;

namespace RestaurantDataNaseGUI.Services;

// Calculul costului si crearea comenzilor pentru clienti autentificati.
public interface IOrderService
{
    // Calculeaza subtotalul, discountul (daca se aplica) si costul de
    // transport pentru cosul dat, fara sa creeze o comanda - util pentru
    // un ecran de "cos" care afiseaza totalul inainte de confirmare.
    Task<CalculComandaDto> CalculeazaCostComandaAsync(
        List<ArticolCosDto> articole,
        int utilizatorId,
        CancellationToken cancellationToken = default);

    // Creeaza o comanda noua pentru utilizatorId (trebuie sa fie clientul
    // autentificat curent), dupa verificarea disponibilitatii articolelor.
    // Calculeaza costurile prin CalculeazaCostComandaAsync si insereaza
    // antetul + liniile intr-o singura tranzactie EF Core.
    Task<OrderResult> CreeazaComandaAsync(
        List<ArticolCosDto> articole,
        int utilizatorId,
        CancellationToken cancellationToken = default);

    // Toate comenzile lui utilizatorId, cu articolele lor, cele mai recente
    // primele. Foloseste GetComenziClientCuDetaliiAsync (un rand per articol)
    // si grupeaza randurile dupa ComandaId.
    Task<List<ComandaClientDto>> GetComenziClientAsync(
        int utilizatorId,
        CancellationToken cancellationToken = default);

    // Anuleaza o comanda activa (nici "livrata", nici deja "anulata") a lui
    // utilizatorId; verifica intai ca ii apartine cu adevarat.
    Task<OrderResult> AnuleazaComandaAsync(
        int comandaId,
        int utilizatorId,
        CancellationToken cancellationToken = default);

    // Toate comenzile tuturor clientilor, cu datele clientului incluse,
    // sortate descrescator dupa data - doar pentru angajati autentificati
    // (altfel arunca UnauthorizedAccessException).
    Task<List<ComandaAngajatDto>> GetToateComenzileAsync(CancellationToken cancellationToken = default);

    // Ca GetToateComenzileAsync, filtrat pe comenzile active (nelivrate, neanulate).
    Task<List<ComandaAngajatDto>> GetComenziActiveAngajatAsync(CancellationToken cancellationToken = default);

    // Starile in care se poate trece direct din stareCurenta (vezi tranzitiile
    // valide din OrderService). Lista goala daca stareCurenta e finala sau necunoscuta.
    IReadOnlyList<string> GetStariUrmatoarePosibile(string stareCurenta);

    // Schimba starea unei comenzi - doar pentru angajati autentificati.
    // Respinge tranzitia daca starea curenta e finala ("livrata"/"anulata")
    // sau daca stareNoua nu e valida din starea curenta (vezi
    // GetStariUrmatoarePosibile). Daca stareNoua e "se pregateste", apeleaza
    // automat UpdateCantitateTotalaLaComandaAsync in aceeasi tranzactie -
    // daca scaderea stocului esueaza, toata schimbarea e anulata.
    Task<OrderResult> SchimbaStareComandaAsync(
        int comandaId,
        string stareNoua,
        CancellationToken cancellationToken = default);
}
