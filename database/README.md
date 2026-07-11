# Schema bazei de date - RestaurantDataNaseGUI

Acest folder conține schema SQL Server 2022 pentru aplicația de comenzi
online de restaurant. Scriptul `schema.sql` creează baza de date
`RestaurantDataNase`, toate tabelele, constrângerile de integritate, o
funcție scalară și 7 proceduri stocate.

Acesta este doar pasul de schemă SQL — entitățile EF Core și `DbContext`-ul
**nu** sunt implementate încă în acest pas.

## Cum se rulează

```bash
sqlcmd -S <server> -U <user> -P <parola> -i database/schema.sql
```

sau se deschide `schema.sql` în SQL Server Management Studio / Azure Data
Studio și se execută integral (F5). Scriptul este idempotent: la început
șterge (dacă există) toate obiectele pe care le creează, deci poate fi
rulat de mai multe ori pe aceeași bază de date fără erori.

## Diagramă relații (pe scurt)

```
Categorie ──1:N── Preparat ──1:N── PreparatImagine
Categorie ──1:N── Meniu
Alergen   ──M:N── Preparat        (prin PreparatAlergen)
Preparat  ──M:N── Meniu           (prin MeniuPreparat, cu CantitateInMeniu)

Utilizator ──1:N── Comanda
StareComanda ──1:N── Comanda
Comanda ──1:N── ComandaDetaliu ──(N:1 XOR)── Preparat | Meniu

Configurare  (independent, folosit de fn_CalculeazaPretMeniu)
```

## Tabele

### Categorie
Categoriile în care se încadrează atât preparatele, cât și meniurile (ex.
"Fel principal", "Desert", "Băutură"). Un singur tabel partajat, folosit de
`Preparat.CategorieId` și `Meniu.CategorieId`, ca să nu existe două
taxonomii paralele pentru același concept.

### Alergen
Lista alergenilor posibili (ex. "Gluten", "Lactoză", "Arahide"). Se leagă de
`Preparat` prin tabelul de legătură `PreparatAlergen`.

### Configurare
Tabel generic cheie-valoare pentru parametri ai aplicației care nu trebuie
hardcodați în cod C# sau în proceduri — toate se citesc din acest tabel la
runtime. Ușor de extins cu alte configurări în viitor fără a modifica
schema. Chei folosite în prezent:

| Cheie                        | Valoare seed | Descriere                                                                 |
|-------------------------------|:------------:|----------------------------------------------------------------------------|
| `DiscountMeniuProcent`        | 10           | Procent de discount aplicat la suma preparatelor componente ale unui Meniu |
| `SumaMinimaComandaDiscount`   | 100          | Suma minimă a unei comenzi (lei) peste care se aplică discountul de frecvență |
| `NumarComenziPentruDiscount`  | 5            | Numărul de comenzi necesare în `IntervalTimpDiscount` zile pentru a deveni client frecvent |
| `IntervalTimpDiscount`        | 30           | Intervalul de timp (zile) în care se numără comenzile pentru discountul de frecvență |
| `ProcentDiscountFrecventa`    | 5            | Procent de discount aplicat comenzilor clienților frecvenți               |
| `PragTransportGratuit`        | 150          | Suma (lei) peste care transportul este gratuit                            |
| `CostTransport`               | 15           | Costul standard al transportului (lei), aplicat sub `PragTransportGratuit`|
| `PragStocEpuizare`            | 10           | Prag implicit de cantitate în stoc sub care un preparat e aproape de epuizare (folosit de `sp_GetPreparateApropiateDeEpuizare`) |

### StareComanda
Tabel lookup pentru cele 5 stări posibile ale unei comenzi: `inregistrata`,
`se pregateste`, `a plecat la client`, `livrata`, `anulata`. `Comanda`
referă acest tabel prin `StareId` în loc să stocheze un șir liber, ceea ce
previne valori inconsistente și permite adăugarea/redenumirea stărilor
dintr-un singur loc.

### Preparat
Un fel de mâncare individual: `Denumire`, `Pret`, `CantitatePortie`,
`UnitateMasura` (g/ml/buc), `CantitateTotalaRestaurant` (stocul curent),
`CategorieId` (FK) și `Disponibil` (bit). CHECK-uri asigură că prețul și
cantitatea porției sunt strict pozitive, iar stocul nu poate fi negativ.

### PreparatAlergen
Tabel de legătură pentru relația many-to-many `Preparat` ↔ `Alergen`. Cheie
primară compusă `(PreparatId, AlergenId)`, `ON DELETE CASCADE` pe ambele
FK-uri (dacă se șterge un preparat sau un alergen, legăturile dispar
automat).

### PreparatImagine
Un preparat poate avea mai multe imagini (relație 1:N). Fiecare rând conține
`PreparatId` (FK) și `CalePoza` (calea/URL-ul imaginii).

### Meniu
Un meniu compus din mai multe preparate, aparținând unei `Categorie`.
**Nu are coloană de preț.** Prețul se calculează dinamic din suma
`Preparat.Pret` pentru toate preparatele componente (o singură dată per
preparat), minus discountul din `Configurare` — vezi funcția
`dbo.fn_CalculeazaPretMeniu`. Motivul: dacă prețul ar fi stocat direct,
ar deveni o dependență derivată/redundantă față de prețurile preparatelor
și discount, care s-ar putea decala în timp (încălcare a principiului
"nicio valoare calculabilă nu se stochează redundant").

### MeniuPreparat
Tabel de legătură many-to-many `Meniu` ↔ `Preparat`, cu atributul propriu
`CantitateInMeniu` — gramajul/porția preparatului respectiv în acel meniu
(ex. 200g cartofi prăjiți), folosit la afișare și la scăderea din stoc
(`sp_UpdateCantitateTotalaLaComanda`). **Nu** este un multiplicator de preț:
`fn_CalculeazaPretMeniu` NU îl folosește în calculul prețului. Cheie primară
compusă `(MeniuId, PreparatId)`.

### Utilizator
Clienți și angajați, diferențiați prin `TipUtilizator` (`Client` sau
`Angajat`, validat printr-un CHECK). `Email` este unic și validat printr-un
CHECK de format simplu. `AdresaLivrare` este opțională (nu are sens pentru
un angajat). `ParolaHash` stochează doar hash-ul parolei, niciodată parola
în clar.

### Comanda
Antetul unei comenzi: `CodUnic` (identificator lizibil, unic), FK către
`Utilizator` și `StareComanda`, `DataComanda`, `CostTransport`, `Discount`
(procent, 0-100) și `OraEstimataLivrare`. CHECK-uri asigură valori
nenegative pentru cost și discount în intervalul valid.

### ComandaDetaliu
O linie dintr-o comandă. Se poate referi fie la un `Preparat`, fie la un
`Meniu` — niciodată la ambele și niciodată la niciunul — impus prin
constrângerea `CK_ComandaDetaliu_PreparatSauMeniu` (XOR pe cele două FK-uri
nullable). `PretUnitarLaComanda` este un **snapshot istoric** al prețului
la momentul plasării comenzii: nu este o încălcare a 3NF, pentru că nu este
o valoare derivabilă din starea *curentă* a bazei de date — prețul
preparatului sau al meniului se poate schimba ulterior, iar o comandă
istorică trebuie să păstreze prețul de atunci pentru facturare/raportare
corectă.

## Convenția de soft-delete pentru Preparat și Meniu

Preparatele și meniurile **nu se șterg fizic** din baza de date odată ce au
fost folosite într-o comandă. Un `DELETE` pe `Preparat` sau `Meniu` ar eșua
oricum din cauza FK-urilor existente din `ComandaDetaliu`
(`FK_ComandaDetaliu_Preparat`, `FK_ComandaDetaliu_Meniu`), care **nu** au
`ON DELETE CASCADE` — și intenționat, pentru a nu pierde istoricul
comenzilor deja plasate.

Din acest motiv, "ștergerea" unui preparat sau meniu din aplicație trebuie
tratată la nivel de business logic (cod C#) ca un `UPDATE Disponibil = 0`,
niciodată ca un `DELETE`. Coloana `Preparat.Disponibil` există deja în
schemă exact pentru acest scop; procedurile care creează linii de comandă
(`sp_AdaugaDetaliuComanda`) deja filtrează după `Disponibil = 1`, iar
interfața trebuie să ascundă/marcheze ca indisponibile preparatele cu
`Disponibil = 0` în loc să le elimine din listă.

Pentru cazul simplu — marcarea unui preparat ca indisponibil — schema oferă
procedura `dbo.sp_SetPreparatIndisponibil` (vezi mai jos). Meniurile nu au o
coloană `Disponibil` proprie momentan; disponibilitatea unui meniu se poate
deriva din disponibilitatea preparatelor sale componente la nivel de
business logic, fără a introduce o coloană redundantă în schemă.

## De ce respectă schema Forma Normală 3

- **1NF**: toate coloanele conțin valori atomice; relațiile many-to-many
  (Preparat↔Alergen, Meniu↔Preparat) sunt extrase în tabele de legătură
  separate, nu în liste/coloane repetate.
- **2NF**: toate tabelele cu chei compuse (`PreparatAlergen`,
  `MeniuPreparat`) au drept unic atribut non-cheie o valoare
  (`CantitateInMeniu`) care depinde de *întreaga* cheie compusă, nu doar de
  o parte din ea.
- **3NF**: nu există dependențe tranzitive între atribute non-cheie. În
  particular, prețul unui `Meniu` **nu** este stocat ca și coloană
  derivată din prețurile preparatelor + discount (ceea ce ar fi creat o
  dependență tranzitivă indirectă), ci este calculat la cerere prin
  `fn_CalculeazaPretMeniu`. Similar, `Categorie.Denumire` și
  `Alergen.Denumire` sunt extrase în tabele proprii, nu duplicate în
  `Preparat`/`Meniu`.

## Funcție și proceduri stocate

### `dbo.fn_CalculeazaPretMeniu(@MeniuId INT) RETURNS DECIMAL(10,2)`
Calculează prețul unui meniu: suma `Preparat.Pret` pentru toate preparatele
componente (o singură dată per preparat, **fără** `CantitateInMeniu` — acea
coloană e gramaj/porție, nu multiplicator de preț), minus procentul din
`Configurare` (cheia `DiscountMeniuProcent`). Este sursa unică a prețului
unui meniu — folosită atât de proceduri, cât și disponibilă pentru
interogări ad-hoc.

### 1. `sp_CreateComanda`
```
@UtilizatorId INT, @CostTransport DECIMAL = 0, @Discount DECIMAL = 0,
@ComandaId INT OUTPUT, @CodUnic VARCHAR(20) OUTPUT
```
Creează antetul unei comenzi noi, cu starea inițială `inregistrata` și un
`CodUnic` generat automat. Liniile de comandă se adaugă ulterior cu
`sp_AdaugaDetaliuComanda`.

### 2. `sp_AdaugaDetaliuComanda`
```
@ComandaId INT, @PreparatId INT = NULL, @MeniuId INT = NULL, @Cantitate DECIMAL
```
Adaugă o linie într-o comandă existentă — pentru un `Preparat` sau pentru
un `Meniu` (exact unul dintre parametri trebuie completat). Prețul unitar
este preluat ca snapshot (`Preparat.Pret` sau
`fn_CalculeazaPretMeniu(@MeniuId)`) la momentul apelului.

### 3. `sp_UpdateCantitateTotalaLaComanda` — interogare complexă
```
@ComandaId INT
```
Scade din stocul preparatelor (`Preparat.CantitateTotalaRestaurant`)
cantitatea consumată de o comandă. Folosește un CTE cu `UNION ALL` pentru a
combina consumul direct (linii `Preparat`) cu consumul indirect (linii
`Meniu`, expandate prin JOIN cu `MeniuPreparat`), apoi un `UPDATE...FROM`
cu JOIN pe rezultatul agregat. Rulează într-o tranzacție cu `TRY/CATCH` și
face rollback dacă stocul ar deveni negativ.

### 4. `sp_GetComenziClientCuDetalii` — interogare complexă
```
@UtilizatorId INT
```
Returnează toate comenzile unui client împreună cu toate liniile de
detaliu, starea comenzii și subtotalul fiecărei linii. Combină `Comanda`,
`StareComanda`, `ComandaDetaliu` și, prin `LEFT JOIN`, fie `Preparat`, fie
`Meniu` (în funcție de ce referă fiecare linie).

### 5. `sp_GetPreparateApropiateDeEpuizare`
```
@PragCantitate DECIMAL(10,2) = NULL
```
Listează preparatele al căror stoc a scăzut sub pragul dat, împreună cu
categoria lor (JOIN simplu) — util pentru alerte de reaprovizionare. Dacă
`@PragCantitate` nu este specificat explicit la apel, procedura preia
valoarea implicită din `dbo.Configurare` (cheia `PragStocEpuizare`) în loc
de o valoare hardcodată; dacă acea cheie lipsește din `Configurare`, se
folosește 10 ca ultimă plasă de siguranță.

### 6. `sp_GetMeniuRestaurantCuAlergeni` — interogare complexă
Returnează toate meniurile din restaurant, cu prețul calculat dinamic
(`fn_CalculeazaPretMeniu`) și lista de alergeni agregată din **toate**
preparatele componente ale fiecărui meniu, folosind `STRING_AGG` peste un
lanț de JOIN-uri `Meniu → MeniuPreparat → Preparat → PreparatAlergen →
Alergen`.

### 7. `sp_SetPreparatIndisponibil`
```
@PreparatId INT
```
Marchează un preparat ca indisponibil (`Disponibil = 0`) — implementarea
soft-delete descrisă în secțiunea
[Convenția de soft-delete](#convenția-de-soft-delete-pentru-preparat-și-meniu)
de mai sus. Nu execută niciun `DELETE`.

## Date seed incluse

Scriptul populează automat:
- `StareComanda`: cele 5 stări (`inregistrata`, `se pregateste`, `a plecat
  la client`, `livrata`, `anulata`).
- `Configurare`: toate cele 8 chei descrise în secțiunea
  [Configurare](#configurare) de mai sus, cu valorile lor implicite.

Restul tabelelor (Categorie, Alergen, Preparat, Meniu, Utilizator etc.) nu
sunt populate — vor fi alimentate din aplicație sau din date de test
separate.
