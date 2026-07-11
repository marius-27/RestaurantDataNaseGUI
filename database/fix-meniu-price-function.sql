/* ============================================================================
   Fix: dbo.fn_CalculeazaPretMeniu calcula gresit pretul unui meniu
   inmultind Preparat.Pret cu MeniuPreparat.CantitateInMeniu.
   CantitateInMeniu este gramajul/portia preparatului in cadrul meniului
   (ex. 200g), NU un multiplicator de pret - se foloseste corect ca gramaj
   la afisare si in sp_UpdateCantitateTotalaLaComanda (scaderea din stoc).
   Pretul corect al unui meniu este suma preturilor preparatelor componente
   (o singura data per preparat), minus discountul din Configurare.

   Acest script recreeaza DOAR functia, cu logica corectata. Nu atinge
   tabele sau date - poate fi rulat direct pe o baza de date deja populata
   (Docker local, K8s) fara pierderi de date.
   ============================================================================ */

USE RestaurantDataNase;
GO

IF OBJECT_ID(N'dbo.fn_CalculeazaPretMeniu', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_CalculeazaPretMeniu;
GO

CREATE FUNCTION dbo.fn_CalculeazaPretMeniu (@MeniuId INT)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @PretBrut DECIMAL(10,2);
    DECLARE @DiscountProcent DECIMAL(5,2);

    -- Suma preturilor preparatelor componente, o singura data per preparat -
    -- CantitateInMeniu NU se foloseste aici (este gramaj, nu multiplicator).
    SELECT @PretBrut = SUM(p.Pret)
    FROM dbo.MeniuPreparat mp
    INNER JOIN dbo.Preparat p ON p.Id = mp.PreparatId
    WHERE mp.MeniuId = @MeniuId;

    SELECT @DiscountProcent = TRY_CAST(Valoare AS DECIMAL(5,2))
    FROM dbo.Configurare
    WHERE Cheie = N'DiscountMeniuProcent';

    IF @PretBrut IS NULL SET @PretBrut = 0;
    IF @DiscountProcent IS NULL SET @DiscountProcent = 0;

    RETURN ROUND(@PretBrut * (1 - @DiscountProcent / 100.0), 2);
END
GO
