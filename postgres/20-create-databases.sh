#!/usr/bin/env bash
set -e

# TODO для создания баз прописать свой вариант
export VARIANT="v1"
export SCRIPT_PATH=/docker-entrypoint-initdb.d/
export PGPASSWORD=postgres
export POSTGRES_USER=postgres
psql -f "$SCRIPT_PATH/scripts/db-$VARIANT.sql"

psql --username "$POSTGRES_USER" -d tickets    -f "$SCRIPT_PATH/scripts/tables-tickets.sql"
psql --username "$POSTGRES_USER" -d flights    -f "$SCRIPT_PATH/scripts/tables-flights.sql"
psql --username "$POSTGRES_USER" -d privileges -f "$SCRIPT_PATH/scripts/tables-privileges.sql"
