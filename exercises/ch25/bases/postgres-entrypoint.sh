#!/usr/bin/env bash
# A small stand-in for the official postgres image's entrypoint: on first start, create the database cluster with the
# password in POSTGRES_PASSWORD (and POSTGRES_USER / POSTGRES_DB), listen on all addresses, then run the server as "postgres".
set -euo pipefail
B=/usr/lib/postgresql/16/bin   # su resets PATH
: "${POSTGRES_PASSWORD:?set POSTGRES_PASSWORD}"; USER_=${POSTGRES_USER:-postgres}; DB=${POSTGRES_DB:-$USER_}
mkdir -p "$PGDATA" /var/run/postgresql && chown -R postgres:postgres "$PGDATA" /var/run/postgresql && chmod 700 "$PGDATA"
if [ ! -s "$PGDATA/PG_VERSION" ]; then
  echo "first start: creating the database in $PGDATA"
  echo "$POSTGRES_PASSWORD" > /tmp/pw && chown postgres /tmp/pw
  su postgres -c "$B/initdb -D '$PGDATA' --username='$USER_' --pwfile=/tmp/pw --auth-host=scram-sha-256 --auth-local=trust" > /dev/null
  rm /tmp/pw
  echo "host all all all scram-sha-256" >> "$PGDATA/pg_hba.conf"
  if [ "$DB" != postgres ]; then
    su postgres -c "$B/pg_ctl -D '$PGDATA' -o '-c listen_addresses=' -w start" > /dev/null
    su postgres -c "psql -q -U '$USER_' -d postgres -c 'CREATE DATABASE \"$DB\"'"
    su postgres -c "$B/pg_ctl -D '$PGDATA' -m fast -w stop" > /dev/null
  fi
else
  echo "database already exists in $PGDATA: keeping it"
fi
exec su postgres -c "exec $B/postgres -D '$PGDATA' -c listen_addresses='*'"
