-- Creeaza login-ul si userul dedicat aplicatiei ("marius"), in loc ca
-- aplicatia sa se conecteze direct cu "sa" (rezervat administrarii/
-- troubleshooting).
--
-- Parola NU este hardcodata aici - se citeste din variabila sqlcmd
-- $(DB_USER_PASSWORD), populata din variabila de mediu DB_USER_PASSWORD
-- (vezi docker/.env si docker/init-db.sh):
--
--   sqlcmd -S localhost -U sa -P "<parola-sa>" \
--     -v DB_USER_PASSWORD="$DB_USER_PASSWORD" -i create-app-user.sql
--
-- Idempotent: poate fi rulat de mai multe ori fara erori.

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'marius')
BEGIN
    CREATE LOGIN marius WITH PASSWORD = '$(DB_USER_PASSWORD)';
END
GO

USE RestaurantDataNase;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'marius')
BEGIN
    CREATE USER marius FOR LOGIN marius;
END
GO

ALTER ROLE db_owner ADD MEMBER marius;
GO
