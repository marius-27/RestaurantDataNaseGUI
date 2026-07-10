#!/usr/bin/env bash
# Porneste containerul SQL Server (daca nu ruleaza deja), asteapta sa fie
# sanatos (healthy) si ruleaza database/schema.sql impotriva lui.
#
# Utilizare:
#   ./docker/init-db.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

ENV_FILE="$SCRIPT_DIR/.env"
if [[ ! -f "$ENV_FILE" ]]; then
    echo "Eroare: $ENV_FILE nu exista." >&2
    echo "Copiaza docker/.env.example in docker/.env si completeaza parola SA." >&2
    exit 1
fi

# shellcheck disable=SC1090
source "$ENV_FILE"

if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
    echo "Eroare: MSSQL_SA_PASSWORD nu este setat in $ENV_FILE." >&2
    exit 1
fi

if [[ -z "${DB_USER_PASSWORD:-}" ]]; then
    echo "Eroare: DB_USER_PASSWORD nu este setat in $ENV_FILE." >&2
    exit 1
fi

CONTAINER_NAME="restaurant-sqlserver"
SCHEMA_FILE="$REPO_ROOT/database/schema.sql"
APP_USER_FILE="$SCRIPT_DIR/create-app-user.sql"

if [[ ! -f "$SCHEMA_FILE" ]]; then
    echo "Eroare: nu gasesc $SCHEMA_FILE." >&2
    exit 1
fi

if [[ ! -f "$APP_USER_FILE" ]]; then
    echo "Eroare: nu gasesc $APP_USER_FILE." >&2
    exit 1
fi

echo "Pornesc containerul SQL Server (docker compose up -d)..."
(cd "$SCRIPT_DIR" && docker compose up -d)

echo "Astept ca SQL Server sa devina 'healthy'..."
MAX_WAIT=90
elapsed=0
until [[ "$(docker inspect -f '{{.State.Health.Status}}' "$CONTAINER_NAME" 2>/dev/null)" == "healthy" ]]; do
    if (( elapsed >= MAX_WAIT )); then
        echo "Eroare: SQL Server nu a devenit healthy in $MAX_WAIT secunde." >&2
        echo "Ultimele loguri din container:" >&2
        docker logs "$CONTAINER_NAME" --tail 50 >&2
        exit 1
    fi
    sleep 3
    elapsed=$((elapsed + 3))
    echo "  ...inca astept ($elapsed s)"
done
echo "SQL Server este pregatit."

echo "Copiez schema.sql in container..."
docker cp "$SCHEMA_FILE" "$CONTAINER_NAME:/tmp/schema.sql"

if docker exec "$CONTAINER_NAME" test -x /opt/mssql-tools18/bin/sqlcmd; then
    SQLCMD_BIN="/opt/mssql-tools18/bin/sqlcmd"
    EXTRA_ARGS=(-C)
else
    SQLCMD_BIN="/opt/mssql-tools/bin/sqlcmd"
    EXTRA_ARGS=()
fi

echo "Rulez database/schema.sql impotriva containerului..."
docker exec "$CONTAINER_NAME" "$SQLCMD_BIN" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" "${EXTRA_ARGS[@]}" -i /tmp/schema.sql

echo "Copiez create-app-user.sql in container..."
docker cp "$APP_USER_FILE" "$CONTAINER_NAME:/tmp/create-app-user.sql"

echo "Creez/actualizez userul aplicatiei ('marius')..."
docker exec "$CONTAINER_NAME" "$SQLCMD_BIN" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" "${EXTRA_ARGS[@]}" -v DB_USER_PASSWORD="$DB_USER_PASSWORD" -i /tmp/create-app-user.sql

echo ""
echo "Schema a fost aplicata cu succes. Baza de date 'RestaurantDataNase' este gata de folosit."
echo "Aplicatia se poate conecta cu userul 'marius' (vezi appsettings.Development.json)."
