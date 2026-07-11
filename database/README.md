# Schema bazei de date - RestaurantDataNaseGUI

Acest folder contine schema SQL Server 2022 pentru aplicatia de comenzi
online de restaurant. Scriptul `schema.sql` creeaza baza de date
`RestaurantDataNase`, toate tabelele, constrangerile de integritate, o
functie scalara si 7 proceduri stocate.

Acesta este doar pasul de schema SQL — entitatile EF Core si `DbContext`-ul
**nu** sunt implementate inca in acest pas.

## Cum se ruleaza

```bash
sqlcmd -S <server> -U <user> -P <parola> -i database/schema.sql
```

sau se deschide `schema.sql` in SQL Server Management Studio / Azure Data
Studio si se executa integral (F5). Scriptul este idempotent: la inceput
sterge (daca exista) toate obiectele pe care le creeaza, deci poate fi
rulat de mai multe ori pe aceeasi baza de date fara erori.

## Diagrama relatii (pe scurt)

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
Categoriile in care se incadreaza atat preparatele, cat si meniurile (ex.
"Fel principal", "Desert", "Bautura"). Un singur tabel partajat, folosit de
`Preparat.CategorieId` si `Meniu.CategorieId`, ca sa nu existe doua
taxonomii paralele pentru acelasi concept.

### Alergen
Lista alergenilor posibili (ex. "Gluten", "Lactoza", "Arahide"). Se leaga de
`Preparat` prin tabelul de legatura `PreparatAlergen`.

### Configurare
Tabel generic cheie-valoare pentru parametri ai aplicatiei care nu trebuie
hardcodati in cod C# sau in proceduri — toate se citesc din acest tabel la
runtime. Usor de extins cu alte configurari in viitor fara a modifica
schema. Chei folosite in prezent:

| Cheie                        | Valoare seed | Descriere                                                                 |
|-------------------------------|:------------:|----------------------------------------------------------------------------|
| `DiscountMeniuProcent`        | 10           | Procent de discount aplicat la suma preparatelor componente ale unui Meniu |
| `SumaMinimaComandaDiscount`   | 100          | Suma minima a unei comenzi (lei) peste care se aplica discountul de frecventa |
| `NumarComenziPentruDiscount`  | 5            | Numarul de comenzi necesare in `IntervalTimpDiscount` zile pentru a deveni client frecvent |
| `IntervalTimpDiscount`        | 30           | Intervalul de timp (zile) in care se numara comenzile pentru discountul de frecventa |
| `ProcentDiscountFrecventa`    | 5            | Procent de discount aplicat comenzilor clientilor frecventi               |
| `PragTransportGratuit`        | 150          | Suma (lei) peste care transportul este gratuit                            |
| `CostTransport`               | 15           | Costul standard al transportului (lei), aplicat sub `PragTransportGratuit`|
| `PragStocEpuizare`            | 10           | Prag implicit de cantitate in stoc sub care un preparat e aproape de epuizare (folosit de `sp_GetPreparateApropiateDeEpuizare`) |

### StareComanda
Tabel lookup pentru cele 5 stari posibile ale unei comenzi: `inregistrata`,
`se pregateste`, `a plecat la client`, `livrata`, `anulata`. `Comanda`
refera acest tabel prin `StareId` in loc sa stocheze un sir liber, ceea ce
previne valori inconsistente si permite adaugarea/redenumirea starilor
dintr-un singur loc.

### Preparat
Un fel de mancare individual: `Denumire`, `Pret`, `CantitatePortie`,
`UnitateMasura` (g/ml/buc), `CantitateTotalaRestaurant` (stocul curent),
`CategorieId` (FK) si `Disponibil` (bit). CHECK-uri asigura ca pretul si
cantitatea portiei sunt strict pozitive, iar stocul nu poate fi negativ.

### PreparatAlergen
Tabel de legatura pentru relatia many-to-many `Preparat` ↔ `Alergen`. Cheie
primara compusa `(PreparatId, AlergenId)`, `ON DELETE CASCADE` pe ambele
FK-uri (daca se sterge un preparat sau un alergen, legaturile dispar
automat).

### PreparatImagine
Un preparat poate avea mai multe imagini (relatie 1:N). Fiecare rand contine
`PreparatId` (FK) si `CalePoza` (calea/URL-ul imaginii).

### Meniu
Un meniu compus din mai multe preparate, apartinand unei `Categorie`.
**Nu are coloana de pret.** Pretul se calculeaza dinamic din suma
`Preparat.Pret` pentru toate preparatele componente (o singura data per
preparat), minus discountul din `Configurare` — vezi functia
`dbo.fn_CalculeazaPretMeniu`. Motivul: daca pretul ar fi stocat direct,
ar deveni o dependenta derivata/redundanta fata de preturile preparatelor
si discount, care s-ar putea decala in timp (incalcare a principiului
"nicio valoare calculabila nu se stocheaza redundant").

### MeniuPreparat
Tabel de legatura many-to-many `Meniu` ↔ `Preparat`, cu atributul propriu
`CantitateInMeniu` — gramajul/portia preparatului respectiv in acel meniu
(ex. 200g cartofi prajiti), folosit la afisare si la scaderea din stoc
(`sp_UpdateCantitateTotalaLaComanda`). **Nu** este un multiplicator de pret:
`fn_CalculeazaPretMeniu` NU il foloseste in calculul pretului. Cheie primara
compusa `(MeniuId, PreparatId)`.

### Utilizator
Clienti si angajati, diferentiati prin `TipUtilizator` (`Client` sau
`Angajat`, validat printr-un CHECK). `Email` este unic si validat printr-un
CHECK de format simplu. `AdresaLivrare` este optionala (nu are sens pentru
un angajat). `ParolaHash` stocheaza doar hash-ul parolei, niciodata parola
in clar.

### Comanda
Antetul unei comenzi: `CodUnic` (identificator lizibil, unic), FK catre
`Utilizator` si `StareComanda`, `DataComanda`, `CostTransport`, `Discount`
(procent, 0-100) si `OraEstimataLivrare`. CHECK-uri asigura valori
nenegative pentru cost si discount in intervalul valid.

### ComandaDetaliu
O linie dintr-o comanda. Se poate referi fie la un `Preparat`, fie la un
`Meniu` — niciodata la ambele si niciodata la niciunul — impus prin
constrangerea `CK_ComandaDetaliu_PreparatSauMeniu` (XOR pe cele doua FK-uri
nullable). `PretUnitarLaComanda` este un **snapshot istoric** al pretului
la momentul plasarii comenzii: nu este o incalcare a 3NF, pentru ca nu este
o valoare derivabila din starea *curenta* a bazei de date — pretul
preparatului sau al meniului se poate schimba ulterior, iar o comanda
istorica trebuie sa pastreze pretul de atunci pentru facturare/raportare
corecta.

## Conventia de soft-delete pentru Preparat si Meniu

Preparatele si meniurile **nu se sterg fizic** din baza de date odata ce au
fost folosite intr-o comanda. Un `DELETE` pe `Preparat` sau `Meniu` ar esua
oricum din cauza FK-urilor existente din `ComandaDetaliu`
(`FK_ComandaDetaliu_Preparat`, `FK_ComandaDetaliu_Meniu`), care **nu** au
`ON DELETE CASCADE` — si intentionat, pentru a nu pierde istoricul
comenzilor deja plasate.

Din acest motiv, "stergerea" unui preparat sau meniu din aplicatie trebuie
tratata la nivel de business logic (cod C#) ca un `UPDATE Disponibil = 0`,
niciodata ca un `DELETE`. Coloana `Preparat.Disponibil` exista deja in
schema exact pentru acest scop; procedurile care creeaza linii de comanda
(`sp_AdaugaDetaliuComanda`) deja filtreaza dupa `Disponibil = 1`, iar
interfata trebuie sa ascunda/marcheze ca indisponibile preparatele cu
`Disponibil = 0` in loc sa le elimine din lista.

Pentru cazul simplu — marcarea unui preparat ca indisponibil — schema ofera
procedura `dbo.sp_SetPreparatIndisponibil` (vezi mai jos). Meniurile nu au o
coloana `Disponibil` proprie momentan; disponibilitatea unui meniu se poate
deriva din disponibilitatea preparatelor sale componente la nivel de
business logic, fara a introduce o coloana redundanta in schema.

## De ce respecta schema Forma Normala 3

- **1NF**: toate coloanele contin valori atomice; relatiile many-to-many
  (Preparat↔Alergen, Meniu↔Preparat) sunt extrase in tabele de legatura
  separate, nu in liste/coloane repetate.
- **2NF**: toate tabelele cu chei compuse (`PreparatAlergen`,
  `MeniuPreparat`) au drept unic atribut non-cheie o valoare
  (`CantitateInMeniu`) care depinde de *intreaga* cheie compusa, nu doar de
  o parte din ea.
- **3NF**: nu exista dependente tranzitive intre atribute non-cheie. In
  particular, pretul unui `Meniu` **nu** este stocat ca si coloana
  derivata din preturile preparatelor + discount (ceea ce ar fi creat o
  dependenta tranzitiva indirecta), ci este calculat la cerere prin
  `fn_CalculeazaPretMeniu`. Similar, `Categorie.Denumire` si
  `Alergen.Denumire` sunt extrase in tabele proprii, nu duplicate in
  `Preparat`/`Meniu`.

## Functie si proceduri stocate

### `dbo.fn_CalculeazaPretMeniu(@MeniuId INT) RETURNS DECIMAL(10,2)`
Calculeaza pretul unui meniu: suma `Preparat.Pret` pentru toate preparatele
componente (o singura data per preparat, **fara** `CantitateInMeniu` — acea
coloana e gramaj/portie, nu multiplicator de pret), minus procentul din
`Configurare` (cheia `DiscountMeniuProcent`). Este sursa unica a pretului
unui meniu — folosita atat de proceduri, cat si disponibila pentru
interogari ad-hoc.

### 1. `sp_CreateComanda`
```
@UtilizatorId INT, @CostTransport DECIMAL = 0, @Discount DECIMAL = 0,
@ComandaId INT OUTPUT, @CodUnic VARCHAR(20) OUTPUT
```
Creeaza antetul unei comenzi noi, cu starea initiala `inregistrata` si un
`CodUnic` generat automat. Liniile de comanda se adauga ulterior cu
`sp_AdaugaDetaliuComanda`.

### 2. `sp_AdaugaDetaliuComanda`
```
@ComandaId INT, @PreparatId INT = NULL, @MeniuId INT = NULL, @Cantitate DECIMAL
```
Adauga o linie intr-o comanda existenta — pentru un `Preparat` sau pentru
un `Meniu` (exact unul dintre parametri trebuie completat). Pretul unitar
este preluat ca snapshot (`Preparat.Pret` sau
`fn_CalculeazaPretMeniu(@MeniuId)`) la momentul apelului.

### 3. `sp_UpdateCantitateTotalaLaComanda` — interogare complexa
```
@ComandaId INT
```
Scade din stocul preparatelor (`Preparat.CantitateTotalaRestaurant`)
cantitatea consumata de o comanda. Foloseste un CTE cu `UNION ALL` pentru a
combina consumul direct (linii `Preparat`) cu consumul indirect (linii
`Meniu`, expandate prin JOIN cu `MeniuPreparat`), apoi un `UPDATE...FROM`
cu JOIN pe rezultatul agregat. Ruleaza intr-o tranzactie cu `TRY/CATCH` si
face rollback daca stocul ar deveni negativ.

### 4. `sp_GetComenziClientCuDetalii` — interogare complexa
```
@UtilizatorId INT
```
Returneaza toate comenzile unui client impreuna cu toate liniile de
detaliu, starea comenzii si subtotalul fiecarei linii. Combina `Comanda`,
`StareComanda`, `ComandaDetaliu` si, prin `LEFT JOIN`, fie `Preparat`, fie
`Meniu` (in functie de ce refera fiecare linie).

### 5. `sp_GetPreparateApropiateDeEpuizare`
```
@PragCantitate DECIMAL(10,2) = NULL
```
Listeaza preparatele al caror stoc a scazut sub pragul dat, impreuna cu
categoria lor (JOIN simplu) — util pentru alerte de reaprovizionare. Daca
`@PragCantitate` nu este specificat explicit la apel, procedura preia
valoarea implicita din `dbo.Configurare` (cheia `PragStocEpuizare`) in loc
de o valoare hardcodata; daca acea cheie lipseste din `Configurare`, se
foloseste 10 ca ultima plasa de siguranta.

### 6. `sp_GetMeniuRestaurantCuAlergeni` — interogare complexa
Returneaza toate meniurile din restaurant, cu pretul calculat dinamic
(`fn_CalculeazaPretMeniu`) si lista de alergeni agregata din **toate**
preparatele componente ale fiecarui meniu, folosind `STRING_AGG` peste un
lant de JOIN-uri `Meniu → MeniuPreparat → Preparat → PreparatAlergen →
Alergen`.

### 7. `sp_SetPreparatIndisponibil`
```
@PreparatId INT
```
Marcheaza un preparat ca indisponibil (`Disponibil = 0`) — implementarea
soft-delete descrisa in sectiunea
[Conventia de soft-delete](#conventia-de-soft-delete-pentru-preparat-si-meniu)
de mai sus. Nu executa niciun `DELETE`.

## Date seed incluse

Scriptul populeaza automat:
- `StareComanda`: cele 5 stari (`inregistrata`, `se pregateste`, `a plecat
  la client`, `livrata`, `anulata`).
- `Configurare`: toate cele 8 chei descrise in sectiunea
  [Configurare](#configurare) de mai sus, cu valorile lor implicite.

Restul tabelelor (Categorie, Alergen, Preparat, Meniu, Utilizator etc.) nu
sunt populate — vor fi alimentate din aplicatie sau din date de test
separate.
